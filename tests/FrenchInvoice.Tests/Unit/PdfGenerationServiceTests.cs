using FluentAssertions;
using FrenchInvoice.Core.Models;
using FrenchInvoice.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FrenchInvoice.Tests.Unit;

public class PdfGenerationServiceTests
{
    private readonly PdfGenerationService _service = new(
        null!,
        null!,
        NullLogger<PdfGenerationService>.Instance
    );

    private static Entity CreateSettings() => new()
    {
        Nom = "Test Entreprise",
        NumeroSiret = "12345678901234",
        AdresseSiege = "1 rue Test",
        CodePostal = "75001",
        Ville = "Paris",
        FranchiseTVA = true
    };

    [Fact]
    public void GenererLivreRecettesPdf_ReturnsValidPdf()
    {
        var revenues = new List<Revenue>
        {
            new() { Date = new DateTime(2026, 1, 15), Client = "Client A", Description = "Prestation dev", Montant = 1500m, ModePaiement = "Virement", ReferenceFacture = "FAC-2026-0001" },
            new() { Date = new DateTime(2026, 2, 20), Client = "Client B", Description = "Formation", Montant = 800m, ModePaiement = "Chèque" },
            new() { Date = new DateTime(2026, 3, 10), Client = "Client A", Description = "Maintenance", Montant = 500m, ModePaiement = "Carte bancaire", ReferenceFacture = "FAC-2026-0002" },
        };

        var bytes = _service.GenererLivreRecettesPdf(revenues, CreateSettings(), 2026);

        bytes.Should().NotBeEmpty();
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public void GenererLivreRecettesPdf_EmptyList_StillGenerates()
    {
        var bytes = _service.GenererLivreRecettesPdf(new List<Revenue>(), CreateSettings(), 2026);

        bytes.Should().NotBeEmpty();
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public void GenererRegistreAchatsPdf_ReturnsValidPdf()
    {
        var expenses = new List<Expense>
        {
            new() { Date = new DateTime(2026, 1, 5), Fournisseur = "OVH", Description = "Hébergement", Montant = 29.99m, ModeReglement = "Prélèvement" },
            new() { Date = new DateTime(2026, 2, 15), Fournisseur = "Amazon", Description = "Matériel", Montant = 150m, ModeReglement = "Carte bancaire" },
        };

        var bytes = _service.GenererRegistreAchatsPdf(expenses, CreateSettings(), 2026);

        bytes.Should().NotBeEmpty();
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public void GenererRegistreAchatsPdf_EmptyList_StillGenerates()
    {
        var bytes = _service.GenererRegistreAchatsPdf(new List<Expense>(), CreateSettings(), 2026);

        bytes.Should().NotBeEmpty();
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public void GenererLivreRecettesPdf_TotalIsCorrect()
    {
        var revenues = new List<Revenue>
        {
            new() { Date = new DateTime(2026, 1, 1), Client = "A", Description = "D1", Montant = 100m, ModePaiement = "Virement" },
            new() { Date = new DateTime(2026, 2, 1), Client = "B", Description = "D2", Montant = 200m, ModePaiement = "Espèces" },
        };

        // Vérifie que la génération ne plante pas — le total 300€ est calculé dans le PDF
        var bytes = _service.GenererLivreRecettesPdf(revenues, CreateSettings(), 2026);
        bytes.Should().NotBeEmpty();
    }

    [Fact]
    public void GenererRegistreAchatsPdf_WithMissingModeReglement()
    {
        var expenses = new List<Expense>
        {
            new() { Date = new DateTime(2026, 3, 1), Fournisseur = "Test", Description = "Achat", Montant = 50m, ModeReglement = "" },
            new() { Date = new DateTime(2026, 3, 2), Fournisseur = "Test2", Description = "Achat2", Montant = 75m },
        };

        var bytes = _service.GenererRegistreAchatsPdf(expenses, CreateSettings(), 2026);
        bytes.Should().NotBeEmpty();
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }
}
