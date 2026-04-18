using FluentAssertions;
using FrenchInvoice.Core.Models;
using FrenchInvoice.Core.Services;

namespace FrenchInvoice.Tests.Unit;

public class BankImportServiceTests
{
    private readonly BankImportService _svc = new();

    // ── Boursobank ──

    [Fact]
    public void Boursobank_ParseCorrectement()
    {
        var csv = """
            dateOp;dateVal;label;category;categoryParent;supplierFound;amount;comment;accountNum;accountLabel;accountbalance
            2026-03-15;2026-03-15;Virement client;Revenus;Professionnels;Client SA;1500,50;RAS;FR76XXX;Compte Pro;12500,00
            2026-03-16;2026-03-16;Achat fournitures;Dépenses;Professionnels;;-250,00;;;;;
            """;

        var result = _svc.ParseCsv("Boursobank", csv);

        result.Should().HaveCount(2);
        result[0].Date.Should().Be(new DateTime(2026, 3, 15));
        result[0].Libelle.Should().Be("Virement client");
        result[0].Montant.Should().Be(1500.50m);
        result[0].Solde.Should().Be(12500m);

        result[1].Montant.Should().Be(-250m);
    }

    [Fact]
    public void Boursobank_GereChampsCotesDansCSV()
    {
        var csv = "dateOp;dateVal;label;category;categoryParent;supplierFound;amount;comment;accountNum;accountLabel;accountbalance\n" +
                  "2026-01-10;2026-01-10;\"Paiement; avec point-virgule\";Cat;Parent;;100,00;;;;;\n";

        var result = _svc.ParseCsv("Boursobank", csv);

        result.Should().HaveCount(1);
        result[0].Libelle.Should().Be("Paiement; avec point-virgule");
    }

    // ── BNP Paribas ──

    [Fact]
    public void BNP_ParseCorrectement()
    {
        var csv = "Date;Libellé;Montant\n15/03/2026;Virement reçu;2000,00\n16/03/2026;Prélèvement;-150,50\n";

        var result = _svc.ParseCsv("BNP Paribas", csv);

        result.Should().HaveCount(2);
        result[0].Date.Should().Be(new DateTime(2026, 3, 15));
        result[0].Libelle.Should().Be("Virement reçu");
        result[0].Montant.Should().Be(2000m);
        result[1].Montant.Should().Be(-150.50m);
    }

    // ── Crédit Mutuel / CIC ──

    [Fact]
    public void CreditMutuel_ParseDebitCredit()
    {
        var csv = "Date;Libellé;Débit;Crédit\n15/03/2026;Virement;;1000,00\n16/03/2026;Prélèvement;250,00;\n";

        var result = _svc.ParseCsv("Crédit Mutuel / CIC", csv);

        result.Should().HaveCount(2);
        result[0].Montant.Should().Be(1000m); // crédit
        result[1].Montant.Should().Be(-250m); // débit négatif
    }

    [Fact]
    public void CreditMutuel_CreditPrioritaireSurDebit()
    {
        // Si les deux colonnes sont remplies, crédit a la priorité
        var csv = "Date;Libellé;Débit;Crédit\n15/03/2026;Opération;100,00;500,00\n";

        var result = _svc.ParseCsv("Crédit Mutuel / CIC", csv);

        result.Should().HaveCount(1);
        result[0].Montant.Should().Be(500m);
    }

    // ── Générique ──

    [Fact]
    public void Generique_ParseAvecConfigParDefaut()
    {
        var csv = "Date;Libellé;Montant\n15/03/2026;Test;500,00\n";

        var result = _svc.ParseCsv("Générique", csv);

        result.Should().HaveCount(1);
        result[0].Libelle.Should().Be("Test");
        result[0].Montant.Should().Be(500m);
    }

    // ── Cas limites ──

    [Fact]
    public void ParseCsv_BanqueInconnue_LèveException()
    {
        var act = () => _svc.ParseCsv("Banque Inconnue", "data");
        act.Should().Throw<ArgumentException>().WithMessage("*Banque Inconnue*");
    }

    [Fact]
    public void ParseCsv_FichierVide_RetourneListeVide()
    {
        var result = _svc.ParseCsv("Boursobank", "dateOp;dateVal;label;category;categoryParent;supplierFound;amount\n");
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseCsv_LignesMalformées_Ignorées()
    {
        var csv = "Date;Libellé;Montant\nceci;n'est;pas;un;csv;valide\n\n";
        var result = _svc.ParseCsv("BNP Paribas", csv);
        result.Should().BeEmpty();
    }

    [Fact]
    public void AvailableBanks_ContientToutesLesBanques()
    {
        _svc.AvailableBanks.Should().HaveCount(4);
        _svc.AvailableBanks.Should().Contain("Boursobank");
        _svc.AvailableBanks.Should().Contain("BNP Paribas");
        _svc.AvailableBanks.Should().Contain("Crédit Mutuel / CIC");
        _svc.AvailableBanks.Should().Contain("Générique");
    }

    // ── ParseWithProfile — Profils dynamiques ──

    [Fact]
    public void Profile_MontantUnique_ParseCorrectement()
    {
        var profile = new CsvMappingProfile
        {
            Separator = ";",
            HeaderRow = 1,
            DateFormat = "dd/MM/yyyy",
            DateColumn = 0,
            LibelleColumn = 1,
            MontantColumn = 2
        };
        var csv = "Date;Libellé;Montant\n15/03/2026;Virement reçu;2000,00\n16/03/2026;Prélèvement;-150,50\n";

        var result = _svc.ParseWithProfile(profile, csv);

        result.Should().HaveCount(2);
        result[0].Date.Should().Be(new DateTime(2026, 3, 15));
        result[0].Libelle.Should().Be("Virement reçu");
        result[0].Montant.Should().Be(2000m);
        result[1].Montant.Should().Be(-150.50m);
    }

    [Fact]
    public void Profile_DebitCredit_ParseCorrectement()
    {
        var profile = new CsvMappingProfile
        {
            Separator = ";",
            HeaderRow = 1,
            DateColumn = 0,
            LibelleColumn = 1,
            MontantColumn = null,
            DebitColumn = 2,
            CreditColumn = 3
        };
        var csv = "Date;Libellé;Débit;Crédit\n15/03/2026;Virement;;1000,00\n16/03/2026;Prélèvement;250,00;\n";

        var result = _svc.ParseWithProfile(profile, csv);

        result.Should().HaveCount(2);
        result[0].Montant.Should().Be(1000m);
        result[1].Montant.Should().Be(-250m);
    }

    [Fact]
    public void Profile_FormatDateISO_ParseCorrectement()
    {
        var profile = new CsvMappingProfile
        {
            Separator = ";",
            HeaderRow = 1,
            DateFormat = "yyyy-MM-dd",
            DateColumn = 0,
            LibelleColumn = 1,
            MontantColumn = 2
        };
        var csv = "date;label;amount\n2026-04-15;Test ISO;750,50\n";

        var result = _svc.ParseWithProfile(profile, csv);

        result.Should().HaveCount(1);
        result[0].Date.Should().Be(new DateTime(2026, 4, 15));
        result[0].Montant.Should().Be(750.50m);
    }

    [Fact]
    public void Profile_AvecSolde_ParseCorrectement()
    {
        var profile = new CsvMappingProfile
        {
            Separator = ";",
            HeaderRow = 1,
            DateFormat = "dd/MM/yyyy",
            DateColumn = 0,
            LibelleColumn = 1,
            MontantColumn = 2,
            SoldeColumn = 3
        };
        var csv = "Date;Lib;Montant;Solde\n15/03/2026;Op;100,00;5000,00\n";

        var result = _svc.ParseWithProfile(profile, csv);

        result.Should().HaveCount(1);
        result[0].Solde.Should().Be(5000m);
    }

    [Fact]
    public void Profile_SéparateurVirgule()
    {
        var profile = new CsvMappingProfile
        {
            Separator = ",",
            HeaderRow = 1,
            DateFormat = "yyyy-MM-dd",
            DateColumn = 0,
            LibelleColumn = 1,
            MontantColumn = 2
        };
        var csv = "date,label,amount\n2026-01-10,Paiement,500.00\n";

        var result = _svc.ParseWithProfile(profile, csv);

        result.Should().HaveCount(1);
        result[0].Libelle.Should().Be("Paiement");
        result[0].Montant.Should().Be(500m);
    }

    [Fact]
    public void Profile_SansEntête()
    {
        var profile = new CsvMappingProfile
        {
            Separator = ";",
            HeaderRow = 0,
            DateFormat = "dd/MM/yyyy",
            DateColumn = 0,
            LibelleColumn = 1,
            MontantColumn = 2
        };
        var csv = "15/03/2026;Opération directe;300,00\n";

        var result = _svc.ParseWithProfile(profile, csv);

        result.Should().HaveCount(1);
        result[0].Libelle.Should().Be("Opération directe");
    }

    [Fact]
    public void Profile_LignesMalformées_Ignorées()
    {
        var profile = new CsvMappingProfile
        {
            Separator = ";",
            HeaderRow = 1,
            DateColumn = 0,
            LibelleColumn = 1,
            MontantColumn = 2
        };
        var csv = "Date;Lib;Montant\nceci;n'est;pas;valide\ndate_invalide;Test;100\n";

        var result = _svc.ParseWithProfile(profile, csv);

        result.Should().BeEmpty();
    }

    // ── Utilitaires ──

    [Fact]
    public void DetectSeparator_Semicolon()
    {
        var csv = "Date;Lib;Montant\n15/03/2026;Test;100\n";
        BankImportService.DetectSeparator(csv).Should().Be(";");
    }

    [Fact]
    public void DetectSeparator_Comma()
    {
        var csv = "date,label,amount\n2026-01-10,Test,100\n";
        BankImportService.DetectSeparator(csv).Should().Be(",");
    }

    [Fact]
    public void PreviewCsv_Retourne5LignesMax()
    {
        var csv = "H1;H2;H3\nA;B;C\nD;E;F\nG;H;I\nJ;K;L\nM;N;O\nP;Q;R\n";
        var result = _svc.PreviewCsv(csv, ";", 1);

        result.Should().HaveCount(5);
        result[0].Should().BeEquivalentTo(new[] { "A", "B", "C" });
    }

    [Fact]
    public void GetHeaders_RetourneEntête()
    {
        var csv = "Date;Libellé;Montant\n15/03/2026;Test;100\n";
        var headers = _svc.GetHeaders(csv, ";", 1);

        headers.Should().NotBeNull();
        headers.Should().BeEquivalentTo(new[] { "Date", "Libellé", "Montant" });
    }

    [Fact]
    public void CreateSystemProfiles_Retourne4Profils()
    {
        var profiles = BankImportService.CreateSystemProfiles(42);

        profiles.Should().HaveCount(4);
        profiles.Should().OnlyContain(p => p.IsSystem && p.EntityId == 42);
        profiles.Select(p => p.Nom).Should().Contain("Boursobank");
        profiles.Select(p => p.Nom).Should().Contain("BNP Paribas");
        profiles.Select(p => p.Nom).Should().Contain("Crédit Mutuel / CIC");
        profiles.Select(p => p.Nom).Should().Contain("Générique");
    }

    // ── ParseRevenuesWithProfile — Import plateformes ──

    [Fact]
    public void Revenue_MontantEtClient_ParseCorrectement()
    {
        var profile = new CsvMappingProfile
        {
            ProfileType = CsvProfileType.Plateforme,
            Separator = ",",
            HeaderRow = 1,
            DateFormat = "yyyy-MM-dd",
            DateColumn = 0,
            LibelleColumn = 1,
            MontantColumn = 2,
            ClientColumn = 3,
            ModePaiementColumn = 4
        };
        var csv = "date,description,amount,customer,method\n2026-04-10,Prestation web,500.00,Dupont SARL,CB\n";

        var result = _svc.ParseRevenuesWithProfile(profile, csv, 1, ActivityCategory.BNC);

        result.Should().HaveCount(1);
        result[0].Revenue.Date.Should().Be(new DateTime(2026, 4, 10));
        result[0].Revenue.Description.Should().Be("Prestation web");
        result[0].Revenue.Montant.Should().Be(500m);
        result[0].Revenue.Client.Should().Be("Dupont SARL");
        result[0].Revenue.ModePaiement.Should().Be("CB");
        result[0].Revenue.Categorie.Should().Be(ActivityCategory.BNC);
        result[0].Revenue.EntityId.Should().Be(1);
    }

    [Fact]
    public void Revenue_AvecFraisEtReference()
    {
        var profile = new CsvMappingProfile
        {
            ProfileType = CsvProfileType.Plateforme,
            Separator = ";",
            HeaderRow = 1,
            DateFormat = "dd/MM/yyyy",
            DateColumn = 0,
            LibelleColumn = 1,
            MontantColumn = 2,
            FraisColumn = 3,
            ReferenceColumn = 4,
            Nom = "PayPal"
        };
        var csv = "Date;Description;Montant;Frais;Ref\n15/04/2026;Paiement reçu;100,00;2,90;TXN123\n";

        var result = _svc.ParseRevenuesWithProfile(profile, csv, 5, ActivityCategory.BICServices);

        result.Should().HaveCount(1);
        result[0].Revenue.Montant.Should().Be(100m);
        result[0].Frais.Should().Be(2.90m);
        result[0].Revenue.ReferenceFacture.Should().Be("CSV:PayPal:TXN123");
        result[0].Revenue.Categorie.Should().Be(ActivityCategory.BICServices);
    }

    [Fact]
    public void Revenue_SansColonnesOptionnelles()
    {
        var profile = new CsvMappingProfile
        {
            ProfileType = CsvProfileType.Plateforme,
            Separator = ";",
            HeaderRow = 1,
            DateColumn = 0,
            LibelleColumn = 1,
            MontantColumn = 2
        };
        var csv = "Date;Lib;Montant\n15/03/2026;Vente;250,00\n";

        var result = _svc.ParseRevenuesWithProfile(profile, csv, 1, ActivityCategory.BNC);

        result.Should().HaveCount(1);
        result[0].Revenue.Client.Should().BeEmpty();
        result[0].Revenue.ModePaiement.Should().Be("En ligne");
        result[0].Revenue.ReferenceFacture.Should().BeNull();
        result[0].Frais.Should().Be(0m);
    }
}
