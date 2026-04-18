using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;
using FrenchInvoice.Core.Models;
using FrenchInvoice.Core.Services;
using FrenchInvoice.Tests.Fixtures;

namespace FrenchInvoice.Tests.Unit;

public class QuoteServiceTests : IDisposable
{
    private readonly DatabaseFixture _db = new();
    private readonly Mock<PdfGenerationService> _pdfMock;

    public QuoteServiceTests()
    {
        _pdfMock = new Mock<PdfGenerationService>(MockBehavior.Loose, (IWebHostEnvironment)null!, (ISecretProvider)null!, (Microsoft.Extensions.Logging.ILogger<PdfGenerationService>)null!);
        _pdfMock.Setup(p => p.GenererDevisPdfAsync(It.IsAny<Quote>(), It.IsAny<Entity>()))
            .ReturnsAsync("Data/quotes/test.pdf");
        _pdfMock.Setup(p => p.GenererFacturePdfAsync(It.IsAny<Invoice>(), It.IsAny<Entity>()))
            .ReturnsAsync("Data/invoices/test.pdf");
    }

    // ── Création ──

    [Fact]
    public async Task CreateAsync_AttribueNuméroImmédiatement()
    {
        var (svc, _, client) = Setup();

        var quote = MakeQuote(client.Id);
        var result = await svc.CreateAsync(quote);

        result.Numero.Should().Be("DEV-2026-0001");
    }

    [Fact]
    public async Task CreateAsync_NumérotationSéquentielle()
    {
        var (svc, _, client) = Setup();

        var q1 = await svc.CreateAsync(MakeQuote(client.Id));
        var q2 = await svc.CreateAsync(MakeQuote(client.Id));

        q1.Numero.Should().Be("DEV-2026-0001");
        q2.Numero.Should().Be("DEV-2026-0002");
    }

    [Fact]
    public async Task CreateAsync_CalculeTotauxAvecFranchiseTVA()
    {
        var (svc, _, client) = Setup(); // FranchiseTVA = true

        var quote = MakeQuote(client.Id);
        quote.Lignes[0].TauxTVA = 20; // sera forcé à 0
        var result = await svc.CreateAsync(quote);

        result.MontantTVA.Should().Be(0m);
        result.MontantTTC.Should().Be(result.MontantHT);
    }

    // ── Transitions de statut ──

    [Theory]
    [InlineData(QuoteStatus.Brouillon, QuoteStatus.Envoye, true)]
    [InlineData(QuoteStatus.Envoye, QuoteStatus.Accepte, true)]
    [InlineData(QuoteStatus.Envoye, QuoteStatus.Refuse, true)]
    [InlineData(QuoteStatus.Envoye, QuoteStatus.Expire, true)]
    [InlineData(QuoteStatus.Brouillon, QuoteStatus.Accepte, false)]
    [InlineData(QuoteStatus.Accepte, QuoteStatus.Brouillon, false)]
    [InlineData(QuoteStatus.Refuse, QuoteStatus.Envoye, false)]
    public async Task ChangerStatutAsync_ValideTransitions(QuoteStatus from, QuoteStatus to, bool valid)
    {
        var (svc, _, client) = Setup();
        var quote = await svc.CreateAsync(MakeQuote(client.Id));

        // Amener au statut de départ
        if (from == QuoteStatus.Envoye)
            await svc.ChangerStatutAsync(quote.Id, QuoteStatus.Envoye);
        else if (from == QuoteStatus.Accepte)
        {
            await svc.ChangerStatutAsync(quote.Id, QuoteStatus.Envoye);
            await svc.ChangerStatutAsync(quote.Id, QuoteStatus.Accepte);
        }
        else if (from == QuoteStatus.Refuse)
        {
            await svc.ChangerStatutAsync(quote.Id, QuoteStatus.Envoye);
            await svc.ChangerStatutAsync(quote.Id, QuoteStatus.Refuse);
        }

        if (valid)
        {
            var result = await svc.ChangerStatutAsync(quote.Id, to);
            result.Statut.Should().Be(to);
        }
        else
        {
            var act = () => svc.ChangerStatutAsync(quote.Id, to);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*non autorisée*");
        }
    }

    // ── Expiration automatique ──

    [Fact]
    public async Task GetAllAsync_MarqueDevisExpirés()
    {
        var (svc, entity, client) = Setup();
        var quote = await svc.CreateAsync(MakeQuote(client.Id));
        await svc.ChangerStatutAsync(quote.Id, QuoteStatus.Envoye);

        // Forcer la date de validité dans le passé
        using var db = _db.CreateDbContext();
        var q = await db.Quotes.FindAsync(quote.Id);
        q!.DateValidite = DateTime.Today.AddDays(-1);
        await db.SaveChangesAsync();

        var quotes = await svc.GetAllAsync();
        quotes.First(x => x.Id == quote.Id).Statut.Should().Be(QuoteStatus.Expire);
    }

    // ── Suppression ──

    [Fact]
    public async Task DeleteAsync_SupprimeBrouillon()
    {
        var (svc, _, client) = Setup();
        var quote = await svc.CreateAsync(MakeQuote(client.Id));

        await svc.DeleteAsync(quote.Id);

        var result = await svc.GetByIdAsync(quote.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_RefuseSuppressionDevisEnvoyé()
    {
        var (svc, _, client) = Setup();
        var quote = await svc.CreateAsync(MakeQuote(client.Id));
        await svc.ChangerStatutAsync(quote.Id, QuoteStatus.Envoye);

        var act = () => svc.DeleteAsync(quote.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*brouillons*");
    }

    // ── Conversion en facture ──

    [Fact]
    public async Task ConvertirEnFactureAsync_CréeFactureBrouillon()
    {
        var (svc, _, client) = Setup();
        var quote = await svc.CreateAsync(MakeQuote(client.Id));
        await svc.ChangerStatutAsync(quote.Id, QuoteStatus.Envoye);
        await svc.ChangerStatutAsync(quote.Id, QuoteStatus.Accepte);

        var invoice = await svc.ConvertirEnFactureAsync(quote.Id);

        invoice.Statut.Should().Be(InvoiceStatus.Brouillon);
        invoice.ClientId.Should().Be(client.Id);
        invoice.Lignes.Should().HaveCount(1);
        invoice.Lignes[0].Description.Should().Be("Prestation");
    }

    [Fact]
    public async Task ConvertirEnFactureAsync_RefuseSiPasAccepté()
    {
        var (svc, _, client) = Setup();
        var quote = await svc.CreateAsync(MakeQuote(client.Id));
        await svc.ChangerStatutAsync(quote.Id, QuoteStatus.Envoye);

        var act = () => svc.ConvertirEnFactureAsync(quote.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*acceptés*");
    }

    [Fact]
    public async Task ConvertirEnFactureAsync_RefuseDoubleConversion()
    {
        var (svc, _, client) = Setup();
        var quote = await svc.CreateAsync(MakeQuote(client.Id));
        await svc.ChangerStatutAsync(quote.Id, QuoteStatus.Envoye);
        await svc.ChangerStatutAsync(quote.Id, QuoteStatus.Accepte);

        await svc.ConvertirEnFactureAsync(quote.Id);

        var act = () => svc.ConvertirEnFactureAsync(quote.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*déjà été converti*");
    }

    // ── Helpers ──

    private (QuoteService svc, Entity entity, Client client) Setup()
    {
        using var db = _db.CreateDbContext();
        var entity = _db.SeedEntity(db);
        var client = _db.SeedClient(db, entity.Id);
        var svc = CreateService(entity.Id);
        return (svc, entity, client);
    }

    private QuoteService CreateService(int entityId)
    {
        var tenant = new TestTenantProvider(entityId);
        var invoiceSvc = new InvoiceService(_db.CreateFactory(), _pdfMock.Object, tenant);
        return new QuoteService(_db.CreateFactory(), _pdfMock.Object, invoiceSvc, tenant);
    }

    private static Quote MakeQuote(int clientId) => new()
    {
        ClientId = clientId,
        DateValidite = DateTime.Today.AddDays(30),
        Lignes = new List<QuoteLine>
        {
            new() { Description = "Prestation", Quantite = 2, PrixUnitaire = 100, TauxTVA = 20 }
        }
    };

    public void Dispose() => _db.Dispose();
}
