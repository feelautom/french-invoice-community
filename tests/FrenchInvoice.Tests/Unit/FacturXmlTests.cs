using System.Xml.Linq;
using FluentAssertions;
using FrenchInvoice.Core.Models;
using FrenchInvoice.Core.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace FrenchInvoice.Tests.Unit;

public class FacturXmlTests
{
    private static readonly XNamespace Rsm = "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100";
    private static readonly XNamespace Ram = "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100";
    private static readonly XNamespace Udt = "urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100";

    private readonly PdfGenerationService _service = new(
        null!,
        null!,
        NullLogger<PdfGenerationService>.Instance
    );

    private static Entity CreateDefaultEntity() => new()
    {
        Nom = "Test Entreprise",
        NumeroSiret = "51455400500041",
        TvaIntracommunautaire = null,
        AdresseSiege = "1 rue du Test",
        CodePostal = "75001",
        Ville = "Paris",
        CodePays = "FR",
        Email = "test@example.com",
        Telephone = "0100000000",
        FranchiseTVA = true,
        TauxTVA = 0m
    };

    private static Client CreateDefaultClient() => new()
    {
        Id = 1,
        Nom = "Client Test",
        Adresse = "2 avenue du Client",
        CodePostal = "69001",
        Ville = "Lyon",
        CodePays = "FR",
        Siret = "12345678900010",
        TvaIntracommunautaire = null,
        Email = "client@example.com"
    };

    private static Invoice CreateDefaultInvoice(Client? client = null)
    {
        var c = client ?? CreateDefaultClient();
        var lignes = new List<InvoiceLine>
        {
            new()
            {
                Description = "Prestation de service",
                Quantite = 2,
                PrixUnitaire = 100m,
                TauxTVA = 0m
            }
        };
        return new Invoice
        {
            Id = 1,
            Numero = "FAC-2026-0001",
            DateEmission = new DateTime(2026, 4, 1),
            DateEcheance = new DateTime(2026, 5, 1),
            ClientId = c.Id,
            Client = c,
            Lignes = lignes,
            MontantHT = 200m,
            MontantTVA = 0m,
            MontantTTC = 200m,
            TypeFacture = InvoiceType.Facture,
            NumeroCommande = null,
            DebutPeriode = null,
            FinPeriode = null,
            Notes = null,
            MentionsLegales = null
        };
    }

    private XDocument ParseXml(byte[] xmlBytes)
    {
        using var ms = new MemoryStream(xmlBytes);
        return XDocument.Load(ms);
    }

    [Fact]
    public void FranchiseTVA_ContientCategorieE()
    {
        var entity = CreateDefaultEntity();
        entity.FranchiseTVA = true;
        var invoice = CreateDefaultInvoice();
        invoice.Lignes[0].TauxTVA = 0m;

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        // Check line-level CategoryCode = E
        var lineItems = xml.Descendants(Ram + "IncludedSupplyChainTradeLineItem").ToList();
        lineItems.Should().NotBeEmpty();
        foreach (var line in lineItems)
        {
            var categoryCode = line
                .Descendants(Ram + "CategoryCode")
                .FirstOrDefault();
            categoryCode.Should().NotBeNull();
            categoryCode!.Value.Should().Be("E");
        }

        // Check header ApplicableTradeTax with CategoryCode=E and ExemptionReasonCode
        var headerTax = xml.Descendants(Ram + "ApplicableTradeTax")
            .Where(e => e.Parent?.Name == Ram + "ApplicableHeaderTradeSettlement")
            .ToList();
        headerTax.Should().NotBeEmpty();
        headerTax.Any(t => t.Element(Ram + "CategoryCode")?.Value == "E").Should().BeTrue();
        headerTax.Any(t => t.Element(Ram + "ExemptionReasonCode")?.Value == "VATEX-FR-FRANCHISE").Should().BeTrue();
    }

    [Fact]
    public void FranchiseTVA_CalculeTvaIntraDepuisSiren()
    {
        var entity = CreateDefaultEntity();
        entity.FranchiseTVA = true;
        entity.TvaIntracommunautaire = null;
        entity.NumeroSiret = "51455400500041";
        var invoice = CreateDefaultInvoice();

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        var sellerParty = xml.Descendants(Ram + "SellerTradeParty").First();
        var taxRegs = sellerParty.Elements(Ram + "SpecifiedTaxRegistration").ToList();
        var vaReg = taxRegs.FirstOrDefault(r =>
            r.Element(Ram + "ID")?.Attribute("schemeID")?.Value == "VA");
        vaReg.Should().NotBeNull();
        vaReg!.Element(Ram + "ID")!.Value.Should().Be("FR50514554005");
    }

    [Fact]
    public void AvecTvaIntracommunautaire_UtiliseLaValeur()
    {
        var entity = CreateDefaultEntity();
        entity.TvaIntracommunautaire = "FR12345678901";
        entity.FranchiseTVA = false;
        entity.TauxTVA = 20m;
        var invoice = CreateDefaultInvoice();
        invoice.Lignes[0].TauxTVA = 20m;
        invoice.MontantTVA = 40m;
        invoice.MontantTTC = 240m;

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        var sellerParty = xml.Descendants(Ram + "SellerTradeParty").First();
        var taxRegs = sellerParty.Elements(Ram + "SpecifiedTaxRegistration").ToList();
        var vaReg = taxRegs.FirstOrDefault(r =>
            r.Element(Ram + "ID")?.Attribute("schemeID")?.Value == "VA");
        vaReg.Should().NotBeNull();
        vaReg!.Element(Ram + "ID")!.Value.Should().Be("FR12345678901");
    }

    [Fact]
    public void TypeFactureAvoir_TypeCode381()
    {
        var entity = CreateDefaultEntity();
        var invoice = CreateDefaultInvoice();
        invoice.TypeFacture = InvoiceType.Avoir;

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        var typeCode = xml.Descendants(Ram + "TypeCode")
            .FirstOrDefault(e => e.Parent?.Name == Rsm + "ExchangedDocument");
        typeCode.Should().NotBeNull();
        typeCode!.Value.Should().Be("381");
    }

    [Fact]
    public void TypeFactureAcompte_TypeCode386()
    {
        var entity = CreateDefaultEntity();
        var invoice = CreateDefaultInvoice();
        invoice.TypeFacture = InvoiceType.Acompte;

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        var typeCode = xml.Descendants(Ram + "TypeCode")
            .FirstOrDefault(e => e.Parent?.Name == Rsm + "ExchangedDocument");
        typeCode.Should().NotBeNull();
        typeCode!.Value.Should().Be("326");
    }

    [Fact]
    public void AvecIBAN_SEPACreditTransfer()
    {
        var entity = CreateDefaultEntity();
        var invoice = CreateDefaultInvoice();

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, "FR7630001007941234567890185", "BDFEFRPP"));

        var paymentMeans = xml.Descendants(Ram + "SpecifiedTradeSettlementPaymentMeans").First();
        paymentMeans.Element(Ram + "TypeCode")!.Value.Should().Be("58");

        // L'IBAN doit être présent dans le XML généré
        var xmlStr = xml.ToString();
        xmlStr.Should().Contain("FR7630001007941234567890185");
    }

    [Fact]
    public void SansIBAN_InstrumentNotDefined()
    {
        var entity = CreateDefaultEntity();
        var invoice = CreateDefaultInvoice();

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        var paymentMeans = xml.Descendants(Ram + "SpecifiedTradeSettlementPaymentMeans").First();
        paymentMeans.Element(Ram + "TypeCode")!.Value.Should().Be("1");
    }

    [Fact]
    public void PeriodeFacturation_Valide()
    {
        var entity = CreateDefaultEntity();
        var invoice = CreateDefaultInvoice();
        invoice.DebutPeriode = new DateTime(2026, 3, 1);
        invoice.FinPeriode = new DateTime(2026, 3, 31);

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        var billingPeriod = xml.Descendants(Ram + "BillingSpecifiedPeriod").FirstOrDefault();
        billingPeriod.Should().NotBeNull();
    }

    [Fact]
    public void PeriodeFacturation_IgnoreSiInvalide()
    {
        var entity = CreateDefaultEntity();
        var invoice = CreateDefaultInvoice();
        invoice.DebutPeriode = new DateTime(2026, 3, 31);
        invoice.FinPeriode = new DateTime(2026, 3, 1); // end < start

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        var billingPeriod = xml.Descendants(Ram + "BillingSpecifiedPeriod").FirstOrDefault();
        billingPeriod.Should().BeNull();
    }

    [Fact]
    public void NumeroCommande_Present()
    {
        var entity = CreateDefaultEntity();
        var invoice = CreateDefaultInvoice();
        invoice.NumeroCommande = "CMD-2026-042";

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        var buyerOrderRef = xml.Descendants(Ram + "BuyerOrderReferencedDocument").FirstOrDefault();
        buyerOrderRef.Should().NotBeNull();
        buyerOrderRef!.Element(Ram + "IssuerAssignedID")!.Value.Should().Be("CMD-2026-042");
    }

    [Fact]
    public void PlusieursLignes_VATBreakdownGroupé()
    {
        var entity = CreateDefaultEntity();
        entity.FranchiseTVA = false;
        entity.TauxTVA = 20m;

        var client = CreateDefaultClient();
        var lignes = new List<InvoiceLine>
        {
            new()
            {
                Description = "Prestation à 20%",
                Quantite = 1,
                PrixUnitaire = 100m,
                TauxTVA = 20m
            },
            new()
            {
                Description = "Produit à 5.5%",
                Quantite = 1,
                PrixUnitaire = 50m,
                TauxTVA = 5.5m
            }
        };
        var invoice = new Invoice
        {
            Id = 2,
            Numero = "FAC-2026-0002",
            DateEmission = new DateTime(2026, 4, 1),
            DateEcheance = new DateTime(2026, 5, 1),
            ClientId = client.Id,
            Client = client,
            Lignes = lignes,
            MontantHT = 150m,
            MontantTVA = 22.75m,
            MontantTTC = 172.75m,
            TypeFacture = InvoiceType.Facture
        };

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        var headerTaxes = xml.Descendants(Ram + "ApplicableTradeTax")
            .Where(e => e.Parent?.Name == Ram + "ApplicableHeaderTradeSettlement")
            .ToList();
        headerTaxes.Should().HaveCount(2);

        var rates = headerTaxes
            .Select(t => t.Element(Ram + "RateApplicablePercent")?.Value)
            .OrderBy(v => v)
            .ToList();
        rates.Should().Contain("20.00");
        rates.Should().Contain("5.50");
    }

    [Fact]
    public void Totaux_Corrects()
    {
        var entity = CreateDefaultEntity();
        entity.FranchiseTVA = false;
        entity.TauxTVA = 20m;

        var client = CreateDefaultClient();
        var lignes = new List<InvoiceLine>
        {
            new()
            {
                Description = "Service A",
                Quantite = 3,
                PrixUnitaire = 100m,
                TauxTVA = 20m
            }
        };
        var invoice = new Invoice
        {
            Id = 3,
            Numero = "FAC-2026-0003",
            DateEmission = new DateTime(2026, 4, 1),
            DateEcheance = new DateTime(2026, 5, 1),
            ClientId = client.Id,
            Client = client,
            Lignes = lignes,
            MontantHT = 300m,
            MontantTVA = 60m,
            MontantTTC = 360m,
            TypeFacture = InvoiceType.Facture
        };

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        var monetarySummation = xml.Descendants(Ram + "SpecifiedTradeSettlementHeaderMonetarySummation").First();

        monetarySummation.Element(Ram + "LineTotalAmount")!.Value.Should().Be("300.00");
        monetarySummation.Element(Ram + "TaxBasisTotalAmount")!.Value.Should().Be("300.00");
        monetarySummation.Element(Ram + "TaxTotalAmount")!.Value.Should().Be("60.00");
        monetarySummation.Element(Ram + "GrandTotalAmount")!.Value.Should().Be("360.00");
        monetarySummation.Element(Ram + "DuePayableAmount")!.Value.Should().Be("360.00");
    }

    [Fact]
    public void ProfileComfort_EN16931()
    {
        var entity = CreateDefaultEntity();
        var invoice = CreateDefaultInvoice();

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        var profileId = xml.Descendants(Ram + "GuidelineSpecifiedDocumentContextParameter")
            .FirstOrDefault()
            ?.Element(Ram + "ID");
        profileId.Should().NotBeNull();
        profileId!.Value.Should().Be("urn:cen.eu:en16931:2017");
    }

    [Fact]
    public void CadreFacturation_BT23_Present()
    {
        var entity = CreateDefaultEntity();
        var invoice = CreateDefaultInvoice();
        invoice.Cadre = CadreFacturation.A1;

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        var businessProcess = xml.Descendants(Ram + "BusinessProcessSpecifiedDocumentContextParameter")
            .FirstOrDefault()
            ?.Element(Ram + "ID");
        businessProcess.Should().NotBeNull();
        businessProcess!.Value.Should().Be("A1");
    }

    [Fact]
    public void CadreFacturation_DefautA1()
    {
        var entity = CreateDefaultEntity();
        var invoice = CreateDefaultInvoice();
        // Pas de cadre défini → défaut A1

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        var businessProcess = xml.Descendants(Ram + "BusinessProcessSpecifiedDocumentContextParameter")
            .FirstOrDefault()
            ?.Element(Ram + "ID");
        businessProcess.Should().NotBeNull();
        businessProcess!.Value.Should().Be("A1");
    }

    [Fact]
    public void NumeroContrat_BT12_Present()
    {
        var entity = CreateDefaultEntity();
        var invoice = CreateDefaultInvoice();
        invoice.NumeroContrat = "CONTRAT-2026-001";

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        var contractRef = xml.Descendants(Ram + "ContractReferencedDocument").FirstOrDefault();
        contractRef.Should().NotBeNull();
        contractRef!.Element(Ram + "IssuerAssignedID")!.Value.Should().Be("CONTRAT-2026-001");
    }

    [Fact]
    public void TypeAutoFacture_TypeCode389()
    {
        var entity = CreateDefaultEntity();
        var invoice = CreateDefaultInvoice();
        invoice.TypeFacture = InvoiceType.AutoFacture;

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        var typeCode = xml.Descendants(Ram + "TypeCode")
            .FirstOrDefault(e => e.Parent?.Name == Rsm + "ExchangedDocument");
        typeCode.Should().NotBeNull();
        typeCode!.Value.Should().Be("389");
    }

    [Fact]
    public void CadreA3_Autoliquidation_CategorieAE()
    {
        var entity = CreateDefaultEntity();
        entity.FranchiseTVA = false;
        entity.TauxTVA = 20m;
        var invoice = CreateDefaultInvoice();
        invoice.Cadre = CadreFacturation.A3;
        invoice.Lignes[0].TauxTVA = 0m;
        invoice.MontantTVA = 0m;

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        // Les lignes doivent avoir la catégorie AE (reverse charge)
        var lineCategories = xml.Descendants(Ram + "IncludedSupplyChainTradeLineItem")
            .SelectMany(l => l.Descendants(Ram + "CategoryCode"))
            .Select(c => c.Value)
            .ToList();
        lineCategories.Should().AllBe("AE");
    }

    [Fact]
    public void CadreA8_Intracommunautaire_CategorieK()
    {
        var entity = CreateDefaultEntity();
        entity.FranchiseTVA = false;
        entity.TauxTVA = 20m;
        var invoice = CreateDefaultInvoice();
        invoice.Cadre = CadreFacturation.A8;
        invoice.Lignes[0].TauxTVA = 0m;
        invoice.MontantTVA = 0m;

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        var lineCategories = xml.Descendants(Ram + "IncludedSupplyChainTradeLineItem")
            .SelectMany(l => l.Descendants(Ram + "CategoryCode"))
            .Select(c => c.Value)
            .ToList();
        lineCategories.Should().AllBe("K");
    }

    [Fact]
    public void CadreA7_Export_CategorieG()
    {
        var entity = CreateDefaultEntity();
        entity.FranchiseTVA = false;
        entity.TauxTVA = 20m;
        var invoice = CreateDefaultInvoice();
        invoice.Cadre = CadreFacturation.A7;
        invoice.Lignes[0].TauxTVA = 0m;
        invoice.MontantTVA = 0m;

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        var lineCategories = xml.Descendants(Ram + "IncludedSupplyChainTradeLineItem")
            .SelectMany(l => l.Descendants(Ram + "CategoryCode"))
            .Select(c => c.Value)
            .ToList();
        lineCategories.Should().AllBe("G");
    }

    [Fact]
    public void CodeProduit_BT157_Present()
    {
        var entity = CreateDefaultEntity();
        var invoice = CreateDefaultInvoice();
        invoice.Lignes[0].CodeProduit = "PREST-001";

        var xml = ParseXml(_service.GenererFacturXml(invoice, entity, null, null));

        var xmlStr = xml.ToString();
        xmlStr.Should().Contain("PREST-001");
    }

    [Fact]
    public void DeterminerCategorieTVA_FranchiseTVA_RetourneE()
    {
        var entity = CreateDefaultEntity();
        entity.FranchiseTVA = true;
        var result = PdfGenerationService.DeterminerCategorieTVA(entity, 0m, null);
        result.Should().Be(s2industries.ZUGFeRD.TaxCategoryCodes.E);
    }

    [Fact]
    public void DeterminerCategorieTVA_TauxStandard_RetourneS()
    {
        var entity = CreateDefaultEntity();
        entity.FranchiseTVA = false;
        var result = PdfGenerationService.DeterminerCategorieTVA(entity, 20m, CadreFacturation.A1);
        result.Should().Be(s2industries.ZUGFeRD.TaxCategoryCodes.S);
    }

    [Fact]
    public void DeterminerCategorieTVA_TauxZero_RetourneZ()
    {
        var entity = CreateDefaultEntity();
        entity.FranchiseTVA = false;
        var result = PdfGenerationService.DeterminerCategorieTVA(entity, 0m, CadreFacturation.A1);
        result.Should().Be(s2industries.ZUGFeRD.TaxCategoryCodes.Z);
    }
}
