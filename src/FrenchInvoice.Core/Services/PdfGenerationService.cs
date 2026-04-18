using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using s2industries.ZUGFeRD;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public class PdfGenerationService
{
    private readonly IWebHostEnvironment _env;
    private readonly ISecretProvider _secretProvider;
    private readonly ILogger<PdfGenerationService> _logger;

    public PdfGenerationService(IWebHostEnvironment env, ISecretProvider secretProvider, ILogger<PdfGenerationService> logger)
    {
        _env = env;
        _secretProvider = secretProvider;
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public virtual async Task<string> GenererFacturePdfAsync(Invoice invoice, Entity settings)
    {
        var dir = Path.Combine(_env.ContentRootPath, "Data", "invoices");
        Directory.CreateDirectory(dir);
        var fileName = $"{invoice.Numero}.pdf";
        var filePath = Path.Combine(dir, fileName);

        // Charger IBAN/BIC depuis le provider de secrets (non bloquant — le PDF sera généré sans coordonnées bancaires si indisponible)
        string? iban = null, bic = null;
        if (!string.IsNullOrEmpty(settings.NumeroSiret))
        {
            try
            {
                iban = await _secretProvider.GetSecretAsync(settings.NumeroSiret, "IBAN");
                bic = await _secretProvider.GetSecretAsync(settings.NumeroSiret, "BIC");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Impossible de charger IBAN/BIC pour le SIRET {Siret} — le PDF sera généré sans coordonnées bancaires", settings.NumeroSiret);
            }
        }

        // Générer le XML Factur-X
        var xmlBytes = GenererFacturXml(invoice, settings, iban, bic);

        // Sauvegarder le XML temporairement (nécessaire pour AddAttachment)
        var xmlPath = Path.Combine(dir, $"{invoice.Numero}.xml");
        await File.WriteAllBytesAsync(xmlPath, xmlBytes);

        // Générer le PDF visuel (PDF/A-3b)
        var tempPdfPath = Path.Combine(dir, $"{invoice.Numero}_temp.pdf");
        GenererPdfVisuel(invoice, settings, tempPdfPath);

        // Embarquer le XML Factur-X dans le PDF via DocumentOperation
        DocumentOperation.LoadFile(tempPdfPath)
            .AddAttachment(new DocumentOperation.DocumentAttachment
            {
                FilePath = xmlPath,
                Key = "factur-x.xml",
                AttachmentName = "factur-x.xml",
                MimeType = "text/xml",
                Relationship = DocumentOperation.DocumentAttachmentRelationship.Alternative,
                Description = "Factur-X XML invoice data"
            })
            .ExtendMetadata(ZugferdXmpMetadata)
            .Save(filePath);

        // Nettoyer le fichier temporaire
        try { File.Delete(tempPdfPath); } catch { }

        return filePath;
    }

    private const string ZugferdXmpMetadata = """
        <rdf:Description rdf:about=""
            xmlns:fx="urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#">
            <fx:DocumentType>INVOICE</fx:DocumentType>
            <fx:DocumentFileName>factur-x.xml</fx:DocumentFileName>
            <fx:Version>1.0</fx:Version>
            <fx:ConformanceLevel>EN 16931</fx:ConformanceLevel>
        </rdf:Description>
        <rdf:Description rdf:about=""
            xmlns:pdfaExtension="http://www.aiim.org/pdfa/ns/extension/"
            xmlns:pdfaSchema="http://www.aiim.org/pdfa/ns/schema#"
            xmlns:pdfaProperty="http://www.aiim.org/pdfa/ns/property#">
            <pdfaExtension:schemas>
                <rdf:Bag>
                    <rdf:li rdf:parseType="Resource">
                        <pdfaSchema:schema>Factur-X PDFA Extension Schema</pdfaSchema:schema>
                        <pdfaSchema:namespaceURI>urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#</pdfaSchema:namespaceURI>
                        <pdfaSchema:prefix>fx</pdfaSchema:prefix>
                        <pdfaSchema:property>
                            <rdf:Seq>
                                <rdf:li rdf:parseType="Resource">
                                    <pdfaProperty:name>DocumentFileName</pdfaProperty:name>
                                    <pdfaProperty:valueType>Text</pdfaProperty:valueType>
                                    <pdfaProperty:category>external</pdfaProperty:category>
                                    <pdfaProperty:description>name of the embedded XML invoice file</pdfaProperty:description>
                                </rdf:li>
                                <rdf:li rdf:parseType="Resource">
                                    <pdfaProperty:name>DocumentType</pdfaProperty:name>
                                    <pdfaProperty:valueType>Text</pdfaProperty:valueType>
                                    <pdfaProperty:category>external</pdfaProperty:category>
                                    <pdfaProperty:description>INVOICE</pdfaProperty:description>
                                </rdf:li>
                                <rdf:li rdf:parseType="Resource">
                                    <pdfaProperty:name>Version</pdfaProperty:name>
                                    <pdfaProperty:valueType>Text</pdfaProperty:valueType>
                                    <pdfaProperty:category>external</pdfaProperty:category>
                                    <pdfaProperty:description>The actual version of the Factur-X XML schema</pdfaProperty:description>
                                </rdf:li>
                                <rdf:li rdf:parseType="Resource">
                                    <pdfaProperty:name>ConformanceLevel</pdfaProperty:name>
                                    <pdfaProperty:valueType>Text</pdfaProperty:valueType>
                                    <pdfaProperty:category>external</pdfaProperty:category>
                                    <pdfaProperty:description>The conformance level of the embedded Factur-X data</pdfaProperty:description>
                                </rdf:li>
                            </rdf:Seq>
                        </pdfaSchema:property>
                    </rdf:li>
                </rdf:Bag>
            </pdfaExtension:schemas>
        </rdf:Description>
        """;

    public virtual async Task<string> GenererDevisPdfAsync(Quote quote, Entity settings)
    {
        var dir = Path.Combine(_env.ContentRootPath, "Data", "quotes");
        Directory.CreateDirectory(dir);
        var fileName = $"{quote.Numero}.pdf";
        var filePath = Path.Combine(dir, fileName);

        var pdfBytes = GenererPdfDevisVisuel(quote, settings);
        await File.WriteAllBytesAsync(filePath, pdfBytes);

        return filePath;
    }

    private void GenererPdfVisuel(Invoice invoice, Entity settings, string outputPath)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(40);
                page.MarginBottom(40);
                page.MarginHorizontal(50);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, invoice, settings));
                page.Content().Element(c => ComposeInvoiceContent(c, invoice, settings));
                page.Footer().Element(ComposeFooter);
            });
        })
        .WithSettings(new DocumentSettings
        {
            PDFA_Conformance = QuestPDF.Infrastructure.PDFA_Conformance.PDFA_3B
        });

        document.GeneratePdf(outputPath);
    }

    private byte[] GenererPdfDevisVisuel(Quote quote, Entity settings)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(40);
                page.MarginBottom(40);
                page.MarginHorizontal(50);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeQuoteHeader(c, quote, settings));
                page.Content().Element(c => ComposeQuoteContent(c, quote, settings));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, Invoice invoice, Entity settings)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(settings.Nom).Bold().FontSize(16);
                if (!string.IsNullOrEmpty(settings.NumeroSiret))
                    col.Item().Text($"SIRET : {settings.NumeroSiret}");
                if (!string.IsNullOrEmpty(settings.TvaIntracommunautaire))
                    col.Item().Text($"TVA Intra : {settings.TvaIntracommunautaire}");
                if (!string.IsNullOrEmpty(settings.AdresseSiege))
                    col.Item().Text(settings.AdresseSiege);
                if (!string.IsNullOrEmpty(settings.Telephone))
                    col.Item().Text($"Tel : {settings.Telephone}");
            });

            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Text($"FACTURE N\u00b0 {invoice.Numero}").Bold().FontSize(14);
                col.Item().Text($"Date d'emission : {invoice.DateEmission:dd/MM/yyyy}");
                col.Item().Text($"Date d'echeance : {invoice.DateEcheance:dd/MM/yyyy}");
            });
        });
    }

    private void ComposeInvoiceContent(IContainer container, Invoice invoice, Entity settings)
    {
        container.PaddingVertical(20).Column(col =>
        {
            // Client
            col.Item().PaddingBottom(15).Background(Colors.Grey.Lighten4).Padding(10).Column(clientCol =>
            {
                clientCol.Item().Text("CLIENT").Bold().FontSize(11);
                clientCol.Item().Text(invoice.Client.Nom).Bold();
                if (!string.IsNullOrEmpty(invoice.Client.Adresse))
                    clientCol.Item().Text(invoice.Client.Adresse);
                if (!string.IsNullOrEmpty(invoice.Client.CodePostal) || !string.IsNullOrEmpty(invoice.Client.Ville))
                    clientCol.Item().Text($"{invoice.Client.CodePostal} {invoice.Client.Ville}");
                if (!string.IsNullOrEmpty(invoice.Client.Pays) && invoice.Client.Pays != "France")
                    clientCol.Item().Text(invoice.Client.Pays);
                if (!string.IsNullOrEmpty(invoice.Client.Siret))
                    clientCol.Item().Text($"SIRET : {invoice.Client.Siret}");
                if (!string.IsNullOrEmpty(invoice.Client.TvaIntracommunautaire))
                    clientCol.Item().Text($"TVA Intra : {invoice.Client.TvaIntracommunautaire}");
            });

            // Tableau des lignes
            col.Item().PaddingBottom(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4); // Description
                    columns.RelativeColumn(1); // Quantite
                    columns.RelativeColumn(1.5f); // PU HT
                    columns.RelativeColumn(1); // TVA %
                    columns.RelativeColumn(1.5f); // Montant HT
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Darken3).Padding(5)
                        .Text("Description").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Grey.Darken3).Padding(5)
                        .Text("Qte").FontColor(Colors.White).Bold().AlignRight();
                    header.Cell().Background(Colors.Grey.Darken3).Padding(5)
                        .Text("PU HT").FontColor(Colors.White).Bold().AlignRight();
                    header.Cell().Background(Colors.Grey.Darken3).Padding(5)
                        .Text("TVA %").FontColor(Colors.White).Bold().AlignRight();
                    header.Cell().Background(Colors.Grey.Darken3).Padding(5)
                        .Text("Montant HT").FontColor(Colors.White).Bold().AlignRight();
                });

                foreach (var ligne in invoice.Lignes)
                {
                    var bgColor = invoice.Lignes.IndexOf(ligne) % 2 == 0
                        ? Colors.White : Colors.Grey.Lighten4;

                    table.Cell().Background(bgColor).Padding(5).Text(ligne.Description);
                    table.Cell().Background(bgColor).Padding(5).Text(ligne.Quantite.ToString("G")).AlignRight();
                    table.Cell().Background(bgColor).Padding(5).Text($"{ligne.PrixUnitaire:N2} \u20ac").AlignRight();
                    table.Cell().Background(bgColor).Padding(5).Text($"{ligne.TauxTVA:N1}%").AlignRight();
                    table.Cell().Background(bgColor).Padding(5).Text($"{ligne.MontantHT:N2} \u20ac").AlignRight();
                }
            });

            // Totaux
            col.Item().AlignRight().Width(250).Column(totCol =>
            {
                totCol.Item().Row(r =>
                {
                    r.RelativeItem().Text("Total HT :").Bold();
                    r.RelativeItem().AlignRight().Text($"{invoice.MontantHT:N2} \u20ac").Bold();
                });

                if (settings.FranchiseTVA)
                {
                    totCol.Item().Row(r =>
                    {
                        r.RelativeItem().Text("TVA :");
                        r.RelativeItem().AlignRight().Text("non applicable");
                    });
                }
                else
                {
                    totCol.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"TVA ({settings.TauxTVA:N1}%) :");
                        r.RelativeItem().AlignRight().Text($"{invoice.MontantTVA:N2} \u20ac");
                    });
                }

                totCol.Item().PaddingTop(5).BorderTop(1).BorderColor(Colors.Black).Row(r =>
                {
                    r.RelativeItem().Text("Total TTC :").Bold().FontSize(12);
                    r.RelativeItem().AlignRight().Text($"{invoice.MontantTTC:N2} \u20ac").Bold().FontSize(12);
                });
            });

            // Notes
            if (!string.IsNullOrEmpty(invoice.Notes))
            {
                col.Item().PaddingTop(20).Column(notesCol =>
                {
                    notesCol.Item().Text("Notes").Bold();
                    notesCol.Item().Text(invoice.Notes);
                });
            }

            // Mentions legales
            col.Item().PaddingTop(20).Column(legalCol =>
            {
                legalCol.Item().Text("Mentions legales").Bold().FontSize(9);
                if (settings.FranchiseTVA)
                    legalCol.Item().Text("TVA non applicable, art. 293B du CGI").FontSize(8);
                legalCol.Item().Text("Penalites de retard : 3 fois le taux d'interet legal").FontSize(8);
                legalCol.Item().Text("Indemnite forfaitaire pour frais de recouvrement : 40 \u20ac").FontSize(8);
                if (!string.IsNullOrEmpty(invoice.MentionsLegales))
                    legalCol.Item().Text(invoice.MentionsLegales).FontSize(8);
            });
        });
    }

    private void ComposeQuoteHeader(IContainer container, Quote quote, Entity settings)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(settings.Nom).Bold().FontSize(16);
                if (!string.IsNullOrEmpty(settings.NumeroSiret))
                    col.Item().Text($"SIRET : {settings.NumeroSiret}");
                if (!string.IsNullOrEmpty(settings.TvaIntracommunautaire))
                    col.Item().Text($"TVA Intra : {settings.TvaIntracommunautaire}");
                if (!string.IsNullOrEmpty(settings.AdresseSiege))
                    col.Item().Text(settings.AdresseSiege);
                if (!string.IsNullOrEmpty(settings.Telephone))
                    col.Item().Text($"Tel : {settings.Telephone}");
            });

            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Text($"DEVIS N\u00b0 {quote.Numero}").Bold().FontSize(14);
                col.Item().Text($"Date d'emission : {quote.DateEmission:dd/MM/yyyy}");
                col.Item().Text($"Date de validite : {quote.DateValidite:dd/MM/yyyy}");
            });
        });
    }

    private void ComposeQuoteContent(IContainer container, Quote quote, Entity settings)
    {
        container.PaddingVertical(20).Column(col =>
        {
            // Client
            col.Item().PaddingBottom(15).Background(Colors.Grey.Lighten4).Padding(10).Column(clientCol =>
            {
                clientCol.Item().Text("CLIENT").Bold().FontSize(11);
                clientCol.Item().Text(quote.Client.Nom).Bold();
                if (!string.IsNullOrEmpty(quote.Client.Adresse))
                    clientCol.Item().Text(quote.Client.Adresse);
                if (!string.IsNullOrEmpty(quote.Client.CodePostal) || !string.IsNullOrEmpty(quote.Client.Ville))
                    clientCol.Item().Text($"{quote.Client.CodePostal} {quote.Client.Ville}");
                if (!string.IsNullOrEmpty(quote.Client.Siret))
                    clientCol.Item().Text($"SIRET : {quote.Client.Siret}");
            });

            // Tableau des lignes
            col.Item().PaddingBottom(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1.5f);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Darken3).Padding(5)
                        .Text("Description").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Grey.Darken3).Padding(5)
                        .Text("Qte").FontColor(Colors.White).Bold().AlignRight();
                    header.Cell().Background(Colors.Grey.Darken3).Padding(5)
                        .Text("PU HT").FontColor(Colors.White).Bold().AlignRight();
                    header.Cell().Background(Colors.Grey.Darken3).Padding(5)
                        .Text("TVA %").FontColor(Colors.White).Bold().AlignRight();
                    header.Cell().Background(Colors.Grey.Darken3).Padding(5)
                        .Text("Montant HT").FontColor(Colors.White).Bold().AlignRight();
                });

                foreach (var ligne in quote.Lignes)
                {
                    var bgColor = quote.Lignes.IndexOf(ligne) % 2 == 0
                        ? Colors.White : Colors.Grey.Lighten4;

                    table.Cell().Background(bgColor).Padding(5).Text(ligne.Description);
                    table.Cell().Background(bgColor).Padding(5).Text(ligne.Quantite.ToString("G")).AlignRight();
                    table.Cell().Background(bgColor).Padding(5).Text($"{ligne.PrixUnitaire:N2} \u20ac").AlignRight();
                    table.Cell().Background(bgColor).Padding(5).Text($"{ligne.TauxTVA:N1}%").AlignRight();
                    table.Cell().Background(bgColor).Padding(5).Text($"{ligne.MontantHT:N2} \u20ac").AlignRight();
                }
            });

            // Totaux
            col.Item().AlignRight().Width(250).Column(totCol =>
            {
                totCol.Item().Row(r =>
                {
                    r.RelativeItem().Text("Total HT :").Bold();
                    r.RelativeItem().AlignRight().Text($"{quote.MontantHT:N2} \u20ac").Bold();
                });

                if (settings.FranchiseTVA)
                {
                    totCol.Item().Row(r =>
                    {
                        r.RelativeItem().Text("TVA :");
                        r.RelativeItem().AlignRight().Text("non applicable");
                    });
                }
                else
                {
                    totCol.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"TVA ({settings.TauxTVA:N1}%) :");
                        r.RelativeItem().AlignRight().Text($"{quote.MontantTVA:N2} \u20ac");
                    });
                }

                totCol.Item().PaddingTop(5).BorderTop(1).BorderColor(Colors.Black).Row(r =>
                {
                    r.RelativeItem().Text("Total TTC :").Bold().FontSize(12);
                    r.RelativeItem().AlignRight().Text($"{quote.MontantTTC:N2} \u20ac").Bold().FontSize(12);
                });
            });

            // Notes
            if (!string.IsNullOrEmpty(quote.Notes))
            {
                col.Item().PaddingTop(20).Column(notesCol =>
                {
                    notesCol.Item().Text("Notes").Bold();
                    notesCol.Item().Text(quote.Notes);
                });
            }

            // Mentions
            col.Item().PaddingTop(20).Column(legalCol =>
            {
                if (settings.FranchiseTVA)
                    legalCol.Item().Text("TVA non applicable, art. 293B du CGI").FontSize(8);
                legalCol.Item().Text("Devis valable jusqu'au " + quote.DateValidite.ToString("dd/MM/yyyy")).FontSize(9).Bold();
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
            text.Span("Page ");
            text.CurrentPageNumber();
            text.Span(" / ");
            text.TotalPages();
        });
    }

    public byte[] GenererLivreRecettesPdf(List<Revenue> revenues, Entity settings, int year)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginTop(30);
                page.MarginBottom(30);
                page.MarginHorizontal(40);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text($"Livre des recettes \u2014 {year}").Bold().FontSize(16);
                    col.Item().Text(settings.Nom).FontSize(11);
                    if (!string.IsNullOrEmpty(settings.NumeroSiret))
                        col.Item().Text($"SIRET : {settings.NumeroSiret}").FontSize(9);
                    col.Item().PaddingBottom(10).Text($"Genere le {DateTime.Now:dd/MM/yyyy}").FontSize(8).FontColor(Colors.Grey.Medium);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.2f); // Date
                        columns.RelativeColumn(1.5f); // Ref facture
                        columns.RelativeColumn(2f);   // Client
                        columns.RelativeColumn(3f);   // Nature
                        columns.RelativeColumn(1.2f); // Montant
                        columns.RelativeColumn(1.5f); // Mode reglement
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Date").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Ref. facture").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Client").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Nature de la prestation").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Montant").FontColor(Colors.White).Bold().AlignRight();
                        header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Mode de reglement").FontColor(Colors.White).Bold();
                    });

                    int i = 0;
                    foreach (var rev in revenues)
                    {
                        var bg = i++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                        table.Cell().Background(bg).Padding(4).Text(rev.Date.ToString("dd/MM/yyyy"));
                        table.Cell().Background(bg).Padding(4).Text(rev.ReferenceFacture ?? "\u2014");
                        table.Cell().Background(bg).Padding(4).Text(rev.Client);
                        table.Cell().Background(bg).Padding(4).Text(rev.Description);
                        table.Cell().Background(bg).Padding(4).AlignRight().Text($"{rev.Montant:N2} \u20ac");
                        table.Cell().Background(bg).Padding(4).Text(string.IsNullOrEmpty(rev.ModePaiement) ? "\u2014" : rev.ModePaiement);
                    }

                    // Ligne total
                    var total = revenues.Sum(r => r.Montant);
                    table.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten2).Padding(5).Text("TOTAL").Bold();
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{total:N2} \u20ac").Bold();
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("");
                });

                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenererRegistreAchatsPdf(List<Expense> expenses, Entity settings, int year)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginTop(30);
                page.MarginBottom(30);
                page.MarginHorizontal(40);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text($"Registre des achats \u2014 {year}").Bold().FontSize(16);
                    col.Item().Text(settings.Nom).FontSize(11);
                    if (!string.IsNullOrEmpty(settings.NumeroSiret))
                        col.Item().Text($"SIRET : {settings.NumeroSiret}").FontSize(9);
                    col.Item().PaddingBottom(10).Text($"Genere le {DateTime.Now:dd/MM/yyyy}").FontSize(8).FontColor(Colors.Grey.Medium);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.2f); // Date
                        columns.RelativeColumn(2f);   // Fournisseur
                        columns.RelativeColumn(3f);   // Nature
                        columns.RelativeColumn(1.2f); // Montant
                        columns.RelativeColumn(1.5f); // Mode reglement
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Date").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Fournisseur").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Nature de l'achat").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Montant").FontColor(Colors.White).Bold().AlignRight();
                        header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Mode de reglement").FontColor(Colors.White).Bold();
                    });

                    int i = 0;
                    foreach (var exp in expenses)
                    {
                        var bg = i++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                        table.Cell().Background(bg).Padding(4).Text(exp.Date.ToString("dd/MM/yyyy"));
                        table.Cell().Background(bg).Padding(4).Text(exp.Fournisseur);
                        table.Cell().Background(bg).Padding(4).Text(exp.Description);
                        table.Cell().Background(bg).Padding(4).AlignRight().Text($"{exp.Montant:N2} \u20ac");
                        table.Cell().Background(bg).Padding(4).Text(string.IsNullOrEmpty(exp.ModeReglement) ? "\u2014" : exp.ModeReglement);
                    }

                    // Ligne total
                    var total = expenses.Sum(e => e.Montant);
                    table.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten2).Padding(5).Text("TOTAL").Bold();
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{total:N2} \u20ac").Bold();
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("");
                });

                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    internal byte[] GenererFacturXml(Invoice invoice, Entity settings, string? iban, string? bic)
    {
        var descriptor = InvoiceDescriptor.CreateInvoice(
            invoice.Numero,
            invoice.DateEmission,
            CurrencyCodes.EUR,
            invoice.Numero);

        // Cadre de facturation (BT-23) — requis reforme e-invoicing sept 2026
        descriptor.BusinessProcess = MapCadreFacturation(invoice.Cadre);

        // Type de facture (380=Facture, 381=Avoir, 386=Acompte, 389=AutoFacture)
        descriptor.Type = invoice.TypeFacture switch
        {
            Models.InvoiceType.Avoir => s2industries.ZUGFeRD.InvoiceType.CreditNote,
            Models.InvoiceType.Acompte => s2industries.ZUGFeRD.InvoiceType.PartialInvoice,
            Models.InvoiceType.AutoFacture => s2industries.ZUGFeRD.InvoiceType.SelfBilledInvoice,
            _ => s2industries.ZUGFeRD.InvoiceType.Invoice
        };

        // Vendeur (emetteur)
        var sellerCountry = MapCountryCode(settings.CodePays);
        var sellerGlobalId = !string.IsNullOrEmpty(settings.NumeroSiret)
            ? new GlobalID(GlobalIDSchemeIdentifiers.SiretCode, settings.NumeroSiret)
            : null;
        descriptor.SetSeller(
            name: settings.Nom,
            postcode: settings.CodePostal,
            city: settings.Ville,
            street: settings.AdresseSiege,
            country: sellerCountry,
            id: null,
            globalID: sellerGlobalId);

        // Identifiant fiscal vendeur (BT-31/BT-32 — requis par BR-E-02 pour categorie TVA "E")
        if (!string.IsNullOrEmpty(settings.TvaIntracommunautaire))
        {
            descriptor.AddSellerTaxRegistration(settings.TvaIntracommunautaire, TaxRegistrationSchemeID.VA);
        }
        else if (settings.FranchiseTVA && !string.IsNullOrEmpty(settings.NumeroSiret) && settings.NumeroSiret.Length >= 9)
        {
            // Auto-entrepreneur sans TVA intra : calcul du n TVA a partir du SIREN
            // Formule : cle = (12 + 3 * (SIREN % 97)) % 97, format FRxx + SIREN
            var siren = settings.NumeroSiret[..9];
            if (long.TryParse(siren, out var sirenNum))
            {
                var cle = (12 + 3 * (sirenNum % 97)) % 97;
                descriptor.AddSellerTaxRegistration($"FR{cle:D2}{siren}", TaxRegistrationSchemeID.VA);
            }
        }
        if (!string.IsNullOrEmpty(settings.Telephone))
            descriptor.SetSellerContact(null, null, null, settings.Telephone, null);
        if (!string.IsNullOrEmpty(settings.Email))
            descriptor.SetSellerElectronicAddress(settings.Email, ElectronicAddressSchemeIdentifiers.ElectronicMailSmtp);

        // Acheteur (client)
        var buyerCountry = MapCountryCode(invoice.Client.CodePays);
        var buyerGlobalId = !string.IsNullOrEmpty(invoice.Client.Siret)
            ? new GlobalID(GlobalIDSchemeIdentifiers.SiretCode, invoice.Client.Siret)
            : null;
        descriptor.SetBuyer(
            name: invoice.Client.Nom,
            postcode: invoice.Client.CodePostal,
            city: invoice.Client.Ville,
            street: invoice.Client.Adresse,
            country: buyerCountry,
            id: null,
            globalID: buyerGlobalId);

        if (!string.IsNullOrEmpty(invoice.Client.TvaIntracommunautaire))
            descriptor.AddBuyerTaxRegistration(invoice.Client.TvaIntracommunautaire, TaxRegistrationSchemeID.VA);
        if (!string.IsNullOrEmpty(invoice.Client.Email))
            descriptor.SetBuyerElectronicAddress(invoice.Client.Email, ElectronicAddressSchemeIdentifiers.ElectronicMailSmtp);

        // Conditions de paiement
        if (!string.IsNullOrEmpty(iban))
        {
            descriptor.SetPaymentMeans(PaymentMeansTypeCodes.SEPACreditTransfer);
            descriptor.AddCreditorFinancialAccount(iban, bic);
        }
        else
        {
            descriptor.SetPaymentMeans(PaymentMeansTypeCodes.InstrumentNotDefined);
        }
        descriptor.AddTradePaymentTerms($"Date d'echeance : {invoice.DateEcheance:dd/MM/yyyy}", invoice.DateEcheance);

        // Numero de bon de commande (BT-13, B2G)
        if (!string.IsNullOrEmpty(invoice.NumeroCommande))
            descriptor.OrderNo = invoice.NumeroCommande;

        // Reference de contrat (BT-12)
        if (!string.IsNullOrEmpty(invoice.NumeroContrat))
            descriptor.ContractReferencedDocument = new ContractReferencedDocument { ID = invoice.NumeroContrat };

        // Periode de facturation (end >= start, sinon on ignore)
        if (invoice.DebutPeriode.HasValue && invoice.FinPeriode.HasValue
            && invoice.FinPeriode.Value >= invoice.DebutPeriode.Value)
        {
            descriptor.BillingPeriodStart = invoice.DebutPeriode.Value;
            descriptor.BillingPeriodEnd = invoice.FinPeriode.Value;
        }

        // Date de livraison (obligatoire pour ApplicableHeaderTradeDelivery)
        descriptor.ActualDeliveryDate = invoice.DateEmission;

        // Lignes de detail
        foreach (var ligne in invoice.Lignes)
        {
            var taxCategory = DeterminerCategorieTVA(settings, ligne.TauxTVA, invoice.Cadre);

            var lineItem = descriptor.AddTradeLineItem(
                name: ligne.Description,
                unitCode: QuantityCodes.C62,
                grossUnitPrice: ligne.PrixUnitaire,
                netUnitPrice: ligne.PrixUnitaire,
                billedQuantity: ligne.Quantite,
                taxType: TaxTypes.VAT,
                categoryCode: taxCategory,
                taxPercent: ligne.TauxTVA);

            // Code produit (BT-157)
            if (!string.IsNullOrEmpty(ligne.CodeProduit))
                lineItem.SellerAssignedID = ligne.CodeProduit;
        }

        // VAT breakdown au niveau header (BG-23, obligatoire)
        if (settings.FranchiseTVA)
        {
            descriptor.AddApplicableTradeTax(
                basisAmount: invoice.MontantHT,
                percent: 0m,
                taxAmount: 0m,
                typeCode: TaxTypes.VAT,
                categoryCode: TaxCategoryCodes.E,
                exemptionReasonCode: TaxExemptionReasonCodes.VATEX_FR_FRANCHISE);
        }
        else
        {
            // Regrouper par taux de TVA et categorie
            var tvaGroups = invoice.Lignes
                .GroupBy(l => new { l.TauxTVA, Cat = DeterminerCategorieTVA(settings, l.TauxTVA, invoice.Cadre) })
                .Select(g => new
                {
                    Taux = g.Key.TauxTVA,
                    Categorie = g.Key.Cat,
                    BaseHT = g.Sum(l => l.MontantHT),
                    MontantTVA = g.Sum(l => l.MontantHT * l.TauxTVA / 100m)
                });

            foreach (var group in tvaGroups)
            {
                var exemptionCode = DeterminerCodeExemption(group.Categorie, invoice.Cadre);
                if (exemptionCode.HasValue)
                {
                    descriptor.AddApplicableTradeTax(
                        basisAmount: group.BaseHT,
                        percent: group.Taux,
                        taxAmount: Math.Round(group.MontantTVA, 2),
                        typeCode: TaxTypes.VAT,
                        categoryCode: group.Categorie,
                        exemptionReasonCode: exemptionCode.Value);
                }
                else
                {
                    descriptor.AddApplicableTradeTax(
                        basisAmount: group.BaseHT,
                        percent: group.Taux,
                        taxAmount: Math.Round(group.MontantTVA, 2),
                        typeCode: TaxTypes.VAT,
                        categoryCode: group.Categorie);
                }
            }
        }

        // Totaux
        descriptor.SetTotals(
            lineTotalAmount: invoice.MontantHT,
            taxBasisAmount: invoice.MontantHT,
            taxTotalAmount: invoice.MontantTVA,
            grandTotalAmount: invoice.MontantTTC,
            duePayableAmount: invoice.MontantTTC);

        // Notes (franchise TVA)
        if (settings.FranchiseTVA)
            descriptor.AddNote("TVA non applicable, art. 293B du CGI");

        using var ms = new MemoryStream();
        descriptor.Save(ms, ZUGFeRDVersion.Version23, Profile.Comfort);
        return ms.ToArray();
    }

    private static CountryCodes MapCountryCode(string? code)
    {
        if (string.IsNullOrEmpty(code)) return CountryCodes.FR;
        return Enum.TryParse<CountryCodes>(code, ignoreCase: true, out var result) ? result : CountryCodes.FR;
    }

    /// <summary>
    /// Determine la categorie TVA EN 16931 en fonction du contexte.
    /// </summary>
    internal static TaxCategoryCodes DeterminerCategorieTVA(Entity settings, decimal tauxTVA, CadreFacturation? cadre)
    {
        if (settings.FranchiseTVA)
            return TaxCategoryCodes.E; // Exempt (franchise TVA art. 293B CGI)

        if (cadre == CadreFacturation.A3 || cadre == CadreFacturation.A4)
            return TaxCategoryCodes.AE; // Autoliquidation (reverse charge)

        if (cadre == CadreFacturation.A8)
            return TaxCategoryCodes.K; // Livraison intracommunautaire

        if (cadre == CadreFacturation.A7)
            return TaxCategoryCodes.G; // Export hors UE

        if (tauxTVA == 0m)
            return TaxCategoryCodes.Z; // Taux zero

        return TaxCategoryCodes.S; // Taux standard
    }

    /// <summary>
    /// Determine le code d'exemption TVA (VATEX) pour les categories non-S.
    /// </summary>
    private static TaxExemptionReasonCodes? DeterminerCodeExemption(TaxCategoryCodes categorie, CadreFacturation? cadre)
    {
        return categorie switch
        {
            TaxCategoryCodes.E => TaxExemptionReasonCodes.VATEX_FR_FRANCHISE,
            TaxCategoryCodes.AE => cadre == CadreFacturation.A4
                ? TaxExemptionReasonCodes.VATEX_FR_AE
                : TaxExemptionReasonCodes.VATEX_EU_AE,
            TaxCategoryCodes.K => TaxExemptionReasonCodes.VATEX_EU_IC,
            TaxCategoryCodes.G => TaxExemptionReasonCodes.VATEX_EU_G,
            _ => null
        };
    }

    /// <summary>
    /// Mappe le cadre de facturation (BT-23) vers le code processus Factur-X.
    /// </summary>
    private static string MapCadreFacturation(CadreFacturation? cadre) => cadre switch
    {
        CadreFacturation.A1 => "A1",
        CadreFacturation.A2 => "A2",
        CadreFacturation.A3 => "A3",
        CadreFacturation.A4 => "A4",
        CadreFacturation.A7 => "A7",
        CadreFacturation.A8 => "A8",
        CadreFacturation.A9 => "A9",
        _ => "A1" // Defaut : B2B domestique
    };
}
