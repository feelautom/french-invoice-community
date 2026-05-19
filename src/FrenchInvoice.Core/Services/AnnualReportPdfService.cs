using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public class AnnualReportPdfService
{
    private readonly IWebHostEnvironment _env;

    public AnnualReportPdfService(IWebHostEnvironment env)
    {
        _env = env;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string GenererPdf(AnnualReport report, Entity entity)
    {
        var dir = Path.Combine(_env.ContentRootPath, "Data", "bilans");
        Directory.CreateDirectory(dir);
        var fileName = $"bilan_{report.Annee}_{entity.Id}.pdf";
        var filePath = Path.Combine(dir, fileName);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(40);
                page.MarginVertical(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, report, entity));
                page.Content().Element(c => ComposeContent(c, report, entity));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf(filePath);

        return filePath;
    }

    private void ComposeHeader(IContainer container, AnnualReport report, Entity entity)
    {
        container.Column(col =>
        {
            col.Item().Text($"Bilan annuel {report.Annee}").FontSize(18).Bold();
            col.Item().Text($"{entity.Nom}").FontSize(12).SemiBold();
            if (!string.IsNullOrEmpty(entity.NumeroSiret))
                col.Item().Text($"SIRET : {entity.NumeroSiret}").FontSize(9);
            col.Item().Text($"Statut : {TaxRuleEngine.GetLibelleStatut(report.StatutJuridique)}").FontSize(9);
            col.Item().Text($"Periode : {report.PeriodeDebut:dd/MM/yyyy} au {report.PeriodeFin:dd/MM/yyyy}").FontSize(9);
            col.Item().PaddingBottom(10).LineHorizontal(1);
        });
    }

    private void ComposeContent(IContainer container, AnnualReport report, Entity entity)
    {
        container.Column(col =>
        {
            col.Spacing(15);

            // Compte de resultat
            col.Item().Text("Compte de resultat").FontSize(14).Bold();
            col.Item().Element(c => ComposeCompteResultat(c, report));

            // Bilan simplifie
            col.Item().PaddingTop(10).Text("Bilan simplifie").FontSize(14).Bold();
            col.Item().Element(c => ComposeBilan(c, report));

            // Indicateurs
            col.Item().PaddingTop(10).Text("Indicateurs cles").FontSize(14).Bold();
            col.Item().Element(c => ComposeIndicateurs(c, report));
        });
    }

    private void ComposeCompteResultat(IContainer container, AnnualReport report)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(1);
            });

            // Produits
            table.Cell().Element(HeaderCell).Text("PRODUITS").Bold();
            table.Cell().Element(HeaderCell).AlignRight().Text("Montant (EUR)").Bold();

            table.Cell().Element(Cell).Text("Chiffre d'affaires");
            table.Cell().Element(Cell).AlignRight().Text(report.ChiffreAffaires.ToString("N2"));

            if (report.AutresProduits != 0)
            {
                table.Cell().Element(Cell).Text("Autres produits");
                table.Cell().Element(Cell).AlignRight().Text(report.AutresProduits.ToString("N2"));
            }

            table.Cell().Element(TotalCell).Text("Total produits").Bold();
            table.Cell().Element(TotalCell).AlignRight().Text(report.TotalProduits.ToString("N2")).Bold();

            // Charges
            table.Cell().Element(HeaderCell).Text("CHARGES").Bold();
            table.Cell().Element(HeaderCell).AlignRight().Text("").Bold();

            table.Cell().Element(Cell).Text("Achats et charges externes");
            table.Cell().Element(Cell).AlignRight().Text(report.AchatsChargesExternes.ToString("N2"));

            table.Cell().Element(Cell).Text("Cotisations sociales");
            table.Cell().Element(Cell).AlignRight().Text(report.CotisationsSociales.ToString("N2"));

            table.Cell().Element(Cell).Text("CFP");
            table.Cell().Element(Cell).AlignRight().Text(report.CFP.ToString("N2"));

            if (report.VersementLiberatoire > 0)
            {
                table.Cell().Element(Cell).Text("Versement liberatoire");
                table.Cell().Element(Cell).AlignRight().Text(report.VersementLiberatoire.ToString("N2"));
            }

            if (report.FraisPlateforme > 0)
            {
                table.Cell().Element(Cell).Text("Frais de plateforme");
                table.Cell().Element(Cell).AlignRight().Text(report.FraisPlateforme.ToString("N2"));
            }

            if (report.AutresCharges > 0)
            {
                table.Cell().Element(Cell).Text("Autres charges (frais variables)");
                table.Cell().Element(Cell).AlignRight().Text(report.AutresCharges.ToString("N2"));
            }

            table.Cell().Element(TotalCell).Text("Total charges").Bold();
            table.Cell().Element(TotalCell).AlignRight().Text(report.TotalCharges.ToString("N2")).Bold();

            // Resultat
            table.Cell().Element(HeaderCell).Text("Resultat d'exploitation").Bold();
            table.Cell().Element(HeaderCell).AlignRight().Text(report.ResultatExploitation.ToString("N2")).Bold();

            if (report.ImpotSurBenefice > 0)
            {
                table.Cell().Element(Cell).Text("Impot sur les societes (IS)");
                table.Cell().Element(Cell).AlignRight().Text($"-{report.ImpotSurBenefice:N2}");
            }

            table.Cell().Element(TotalCell).Text("RESULTAT NET").Bold();
            table.Cell().Element(TotalCell).AlignRight().Text(report.ResultatNet.ToString("N2")).Bold();
        });
    }

    private void ComposeBilan(IContainer container, AnnualReport report)
    {
        container.Row(row =>
        {
            row.RelativeItem().Element(c =>
            {
                c.Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                    });

                    table.Cell().ColumnSpan(2).Element(HeaderCell).Text("ACTIF").Bold();

                    table.Cell().Element(Cell).Text("Tresorerie");
                    table.Cell().Element(Cell).AlignRight().Text(report.TresorerieFinExercice.ToString("N2"));

                    table.Cell().Element(Cell).Text("Creances clients");
                    table.Cell().Element(Cell).AlignRight().Text(report.CreancesClients.ToString("N2"));

                    var totalActif = report.TresorerieFinExercice + report.CreancesClients;
                    table.Cell().Element(TotalCell).Text("Total actif").Bold();
                    table.Cell().Element(TotalCell).AlignRight().Text(totalActif.ToString("N2")).Bold();
                });
            });

            row.ConstantItem(20);

            row.RelativeItem().Element(c =>
            {
                c.Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                    });

                    table.Cell().ColumnSpan(2).Element(HeaderCell).Text("PASSIF").Bold();

                    table.Cell().Element(Cell).Text("Apports personnels");
                    table.Cell().Element(Cell).AlignRight().Text(report.CapitalApports.ToString("N2"));

                    table.Cell().Element(Cell).Text("Resultat exercice");
                    table.Cell().Element(Cell).AlignRight().Text(report.ResultatExercice.ToString("N2"));

                    table.Cell().Element(Cell).Text("Dettes URSSAF");
                    table.Cell().Element(Cell).AlignRight().Text(report.DettesUrssaf.ToString("N2"));

                    var totalPassif = report.CapitalApports + report.ResultatExercice + report.DettesUrssaf;
                    table.Cell().Element(TotalCell).Text("Total passif").Bold();
                    table.Cell().Element(TotalCell).AlignRight().Text(totalPassif.ToString("N2")).Bold();
                });
            });
        });
    }

    private void ComposeIndicateurs(IContainer container, AnnualReport report)
    {
        var margeNette = report.ChiffreAffaires > 0
            ? (report.ResultatNet / report.ChiffreAffaires * 100)
            : 0;
        var tauxCharges = report.ChiffreAffaires > 0
            ? (report.TotalCharges / report.ChiffreAffaires * 100)
            : 0;

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(1);
            });

            table.Cell().Element(Cell).Text("Marge nette");
            table.Cell().Element(Cell).AlignRight().Text($"{margeNette:N1} %");

            table.Cell().Element(Cell).Text("Taux de charges");
            table.Cell().Element(Cell).AlignRight().Text($"{tauxCharges:N1} %");

            table.Cell().Element(Cell).Text("Resultat net");
            table.Cell().Element(Cell).AlignRight().Text($"{report.ResultatNet:N2} EUR");
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f);
            col.Item().PaddingTop(5).AlignCenter().Text("Document interne - ne constitue pas une liasse fiscale officielle")
                .FontSize(7).FontColor(Colors.Grey.Medium);
            col.Item().AlignCenter().Text(text =>
            {
                text.Span("Page ").FontSize(7);
                text.CurrentPageNumber().FontSize(7);
                text.Span(" / ").FontSize(7);
                text.TotalPages().FontSize(7);
            });
        });
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Lighten3).Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten1);

    private static IContainer Cell(IContainer container) =>
        container.Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);

    private static IContainer TotalCell(IContainer container) =>
        container.Background(Colors.Grey.Lighten4).Padding(5).BorderTop(1).BorderColor(Colors.Grey.Darken1);
}
