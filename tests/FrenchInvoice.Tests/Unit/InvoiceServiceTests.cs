using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;
using FrenchInvoice.Core.Models;
using FrenchInvoice.Core.Services;
using FrenchInvoice.Tests.Fixtures;

namespace FrenchInvoice.Tests.Unit;

public class InvoiceServiceTests : IDisposable
{
    private readonly DatabaseFixture _db = new();
    private readonly Mock<PdfGenerationService> _pdfMock;

    public InvoiceServiceTests()
    {
        _pdfMock = new Mock<PdfGenerationService>(MockBehavior.Loose, (IWebHostEnvironment)null!, (ISecretProvider)null!, (Microsoft.Extensions.Logging.ILogger<PdfGenerationService>)null!);
        _pdfMock.Setup(p => p.GenererFacturePdfAsync(It.IsAny<Invoice>(), It.IsAny<Entity>()))
            .ReturnsAsync("Data/invoices/test.pdf");
    }

    // ── Création ──

    [Fact]
    public async Task CreateAsync_CréeBrouillonSansNuméro()
    {
        var (svc, entity, client) = Setup();

        var invoice = MakeInvoice(client.Id);
        var result = await svc.CreateAsync(invoice);

        result.Statut.Should().Be(InvoiceStatus.Brouillon);
        result.Numero.Should().BeEmpty();
        result.EntityId.Should().Be(entity.Id);
    }

    [Fact]
    public async Task CreateAsync_CalculeTotaux()
    {
        var (svc, _, client) = Setup();

        var invoice = MakeInvoice(client.Id);
        var result = await svc.CreateAsync(invoice);

        result.MontantHT.Should().Be(200m); // 2 * 100
        result.MontantTVA.Should().Be(40m);  // 200 * 20%
        result.MontantTTC.Should().Be(240m);
    }

    // ── Modification ──

    [Fact]
    public async Task UpdateAsync_ModifieBrouillon()
    {
        var (svc, _, client) = Setup();
        var invoice = await svc.CreateAsync(MakeInvoice(client.Id));

        invoice.Notes = "Mise à jour";
        invoice.Lignes = new List<InvoiceLine>
        {
            new() { Description = "Nouvelle ligne", Quantite = 1, PrixUnitaire = 500, TauxTVA = 20 }
        };
        var updated = await svc.UpdateAsync(invoice);

        updated.Notes.Should().Be("Mise à jour");
        updated.MontantHT.Should().Be(500m);
    }

    [Fact]
    public async Task UpdateAsync_RefuseModificationFactureEnvoyée()
    {
        var (svc, _, client) = Setup();
        var invoice = await svc.CreateAsync(MakeInvoice(client.Id));
        await svc.FinaliserAsync(invoice.Id);

        invoice.Notes = "Tentative";
        var act = () => svc.UpdateAsync(invoice);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*brouillons*");
    }

    // ── Suppression ──

    [Fact]
    public async Task DeleteAsync_SupprimeBrouillon()
    {
        var (svc, _, client) = Setup();
        var invoice = await svc.CreateAsync(MakeInvoice(client.Id));

        await svc.DeleteAsync(invoice.Id);

        var result = await svc.GetByIdAsync(invoice.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_RefuseSuppressionFactureAvecNuméro()
    {
        var (svc, _, client) = Setup();
        var invoice = await svc.CreateAsync(MakeInvoice(client.Id));
        await svc.FinaliserAsync(invoice.Id);

        var act = () => svc.DeleteAsync(invoice.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*brouillons sans numéro*");
    }

    // ── Finalisation ──

    [Fact]
    public async Task FinaliserAsync_AttribueNuméroSéquentiel()
    {
        var (svc, _, client) = Setup();
        var inv1 = await svc.CreateAsync(MakeInvoice(client.Id));
        var inv2 = await svc.CreateAsync(MakeInvoice(client.Id));

        var f1 = await svc.FinaliserAsync(inv1.Id);
        var f2 = await svc.FinaliserAsync(inv2.Id);

        f1.Numero.Should().Be("FAC-2026-0001");
        f2.Numero.Should().Be("FAC-2026-0002");
    }

    [Fact]
    public async Task FinaliserAsync_PasseEnEnvoyée()
    {
        var (svc, _, client) = Setup();
        var invoice = await svc.CreateAsync(MakeInvoice(client.Id));

        var result = await svc.FinaliserAsync(invoice.Id);

        result.Statut.Should().Be(InvoiceStatus.Envoyee);
        result.Numero.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FinaliserAsync_RefuseSiSansLignes()
    {
        var (svc, _, client) = Setup();
        var invoice = new Invoice
        {
            ClientId = client.Id,
            Lignes = new List<InvoiceLine>()
        };
        var created = await svc.CreateAsync(invoice);

        var act = () => svc.FinaliserAsync(created.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*au moins une ligne*");
    }

    [Fact]
    public async Task FinaliserAsync_RefuseSiDéjàFinalisée()
    {
        var (svc, _, client) = Setup();
        var invoice = await svc.CreateAsync(MakeInvoice(client.Id));
        await svc.FinaliserAsync(invoice.Id);

        var act = () => svc.FinaliserAsync(invoice.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*brouillons*");
    }

    [Fact]
    public async Task FinaliserAsync_AppliqueFranchiseTVA()
    {
        var (svc, _, client) = Setup(); // FranchiseTVA = true par défaut

        var invoice = MakeInvoice(client.Id);
        invoice.Lignes[0].TauxTVA = 20; // sera mis à 0 par la franchise
        var created = await svc.CreateAsync(invoice);
        var finalized = await svc.FinaliserAsync(created.Id);

        finalized.MontantTVA.Should().Be(0m);
        finalized.MontantTTC.Should().Be(finalized.MontantHT);
    }

    // ── Marquer payée ──

    [Fact]
    public async Task MarquerPayeeAsync_CréeRevenueAuto()
    {
        var (svc, entity, client) = Setup();
        var invoice = await svc.CreateAsync(MakeInvoice(client.Id));
        var finalized = await svc.FinaliserAsync(invoice.Id);

        var paid = await svc.MarquerPayeeAsync(finalized.Id);

        paid.Statut.Should().Be(InvoiceStatus.Payee);
        paid.RevenueId.Should().NotBeNull();

        // Vérifier la Revenue créée
        using var db = _db.CreateDbContext();
        var revenue = await db.Revenues.FindAsync(paid.RevenueId);
        revenue.Should().NotBeNull();
        revenue!.Montant.Should().Be(finalized.MontantTTC);
        revenue.ReferenceFacture.Should().Be(finalized.Numero);
        revenue.EntityId.Should().Be(entity.Id);
    }

    [Fact]
    public async Task MarquerPayeeAsync_RefuseSiPasBrouillon()
    {
        var (svc, _, client) = Setup();
        var invoice = await svc.CreateAsync(MakeInvoice(client.Id));

        var act = () => svc.MarquerPayeeAsync(invoice.Id); // brouillon, pas envoyée
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*envoyées*");
    }

    // ── Annulation ──

    [Fact]
    public async Task AnnulerAsync_PasseEnAnnulée()
    {
        var (svc, _, client) = Setup();
        var invoice = await svc.CreateAsync(MakeInvoice(client.Id));
        await svc.FinaliserAsync(invoice.Id);

        var cancelled = await svc.AnnulerAsync(invoice.Id);
        cancelled.Statut.Should().Be(InvoiceStatus.Annulee);
    }

    [Fact]
    public async Task AnnulerAsync_RefuseSiBrouillon()
    {
        var (svc, _, client) = Setup();
        var invoice = await svc.CreateAsync(MakeInvoice(client.Id));

        var act = () => svc.AnnulerAsync(invoice.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*envoyées*");
    }

    // ── Calcul totaux ──

    [Fact]
    public void CalculerTotaux_PlusieursLignesTauxDifférents()
    {
        var svc = CreateService(1);
        var invoice = new Invoice
        {
            Lignes = new List<InvoiceLine>
            {
                new() { Description = "A", Quantite = 2, PrixUnitaire = 100, TauxTVA = 20 },
                new() { Description = "B", Quantite = 1, PrixUnitaire = 50, TauxTVA = 5.5m }
            }
        };

        svc.CalculerTotaux(invoice);

        invoice.MontantHT.Should().Be(250m); // 200 + 50
        invoice.MontantTVA.Should().Be(42.75m); // 40 + 2.75
        invoice.MontantTTC.Should().Be(292.75m);
    }

    // ── Isolation multi-tenant ──

    [Fact]
    public async Task GetAllAsync_NeVoitPasAutreEntité()
    {
        using var db = _db.CreateDbContext();
        var entity1 = _db.SeedEntity(db, "Entity 1");
        var entity2 = _db.SeedEntity(db, "Entity 2");
        var client1 = _db.SeedClient(db, entity1.Id, "Client E1");
        var client2 = _db.SeedClient(db, entity2.Id, "Client E2");

        var svc1 = CreateService(entity1.Id);
        var svc2 = CreateService(entity2.Id);

        await svc1.CreateAsync(MakeInvoice(client1.Id));
        await svc2.CreateAsync(MakeInvoice(client2.Id));
        await svc2.CreateAsync(MakeInvoice(client2.Id));

        var invoices1 = await svc1.GetAllAsync();
        var invoices2 = await svc2.GetAllAsync();

        invoices1.Should().HaveCount(1);
        invoices2.Should().HaveCount(2);
    }

    // ── Helpers ──

    private (InvoiceService svc, Entity entity, Client client) Setup()
    {
        using var db = _db.CreateDbContext();
        var entity = _db.SeedEntity(db);
        var client = _db.SeedClient(db, entity.Id);
        var svc = CreateService(entity.Id);
        return (svc, entity, client);
    }

    private InvoiceService CreateService(int entityId)
    {
        return new InvoiceService(_db.CreateFactory(), _pdfMock.Object, new TestTenantProvider(entityId));
    }

    private static Invoice MakeInvoice(int clientId) => new()
    {
        ClientId = clientId,
        Lignes = new List<InvoiceLine>
        {
            new() { Description = "Prestation", Quantite = 2, PrixUnitaire = 100, TauxTVA = 20 }
        }
    };

    public void Dispose() => _db.Dispose();
}
