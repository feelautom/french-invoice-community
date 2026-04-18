using Microsoft.EntityFrameworkCore;
using FrenchInvoice.Core.Data;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public class QuoteService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly PdfGenerationService _pdfService;
    private readonly InvoiceService _invoiceService;
    private readonly ITenantProvider _tenant;

    public QuoteService(IDbContextFactory<AppDbContext> factory, PdfGenerationService pdfService,
        InvoiceService invoiceService, ITenantProvider tenant)
    {
        _factory = factory;
        _pdfService = pdfService;
        _invoiceService = invoiceService;
        _tenant = tenant;
    }

    public async Task<List<Quote>> GetAllAsync(QuoteStatus? statut = null, int? clientId = null,
        DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        var query = db.Quotes.Include(q => q.Client).Include(q => q.Lignes)
            .Where(q => q.EntityId == _tenant.EntityId)
            .AsQueryable();

        if (statut.HasValue)
            query = query.Where(q => q.Statut == statut.Value);
        if (clientId.HasValue)
            query = query.Where(q => q.ClientId == clientId.Value);
        if (dateFrom.HasValue)
            query = query.Where(q => q.DateEmission >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(q => q.DateEmission <= dateTo.Value);

        var quotes = await query.OrderByDescending(q => q.DateEmission).ThenByDescending(q => q.Id).ToListAsync();

        // Marquer les devis expires directement en base (batch atomique)
        var now = DateTime.UtcNow;
        using var db2 = _factory.CreateDbContext();
        var expiredCount = await db2.Quotes
            .Where(q => q.EntityId == _tenant.EntityId
                && q.Statut == QuoteStatus.Envoye
                && q.DateValidite < DateTime.Today)
            .ExecuteUpdateAsync(s => s
                .SetProperty(q => q.Statut, QuoteStatus.Expire)
                .SetProperty(q => q.UpdatedAt, now));

        // Mettre a jour les objets en memoire si des expirations ont eu lieu
        if (expiredCount > 0)
        {
            foreach (var quote in quotes.Where(q => q.Statut == QuoteStatus.Envoye && q.DateValidite < DateTime.Today))
            {
                quote.Statut = QuoteStatus.Expire;
                quote.UpdatedAt = now;
            }
        }

        return quotes;
    }

    public async Task<Quote?> GetByIdAsync(int id)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        return await db.Quotes.Include(q => q.Client).Include(q => q.Lignes)
            .Where(q => q.EntityId == _tenant.EntityId)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<Quote> CreateAsync(Quote quote)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        var settings = await db.Entities.FirstAsync(e => e.Id == _tenant.EntityId);

        // Les devis recoivent un numero immediatement
        quote.Numero = $"{settings.PrefixeDevis}{settings.ProchainNumeroDevis:D4}";
        settings.ProchainNumeroDevis++;

        CalculerTotaux(quote, settings);
        quote.EntityId = _tenant.EntityId;
        quote.CreatedAt = DateTime.UtcNow;
        quote.UpdatedAt = DateTime.UtcNow;

        db.Quotes.Add(quote);
        await db.SaveChangesAsync();

        return await GetByIdAsync(quote.Id) ?? quote;
    }

    public async Task<Quote> UpdateAsync(Quote quote)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        var existing = await db.Quotes.Include(q => q.Lignes)
            .Where(q => q.EntityId == _tenant.EntityId)
            .FirstOrDefaultAsync(q => q.Id == quote.Id)
            ?? throw new InvalidOperationException("Devis introuvable.");

        if (existing.Statut != QuoteStatus.Brouillon && existing.Statut != QuoteStatus.Envoye)
            throw new InvalidOperationException("Ce devis ne peut plus etre modifie.");

        var settings = await db.Entities.FirstAsync(e => e.Id == _tenant.EntityId);

        existing.ClientId = quote.ClientId;
        existing.DateEmission = quote.DateEmission;
        existing.DateValidite = quote.DateValidite;
        existing.Notes = quote.Notes;
        existing.MentionsLegales = quote.MentionsLegales;

        db.QuoteLines.RemoveRange(existing.Lignes);
        existing.Lignes = quote.Lignes.Select(l => new QuoteLine
        {
            Description = l.Description,
            Quantite = l.Quantite,
            PrixUnitaire = l.PrixUnitaire,
            TauxTVA = l.TauxTVA
        }).ToList();

        CalculerTotaux(existing, settings);
        existing.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await GetByIdAsync(existing.Id) ?? existing;
    }

    public async Task DeleteAsync(int id)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        var quote = await db.Quotes.Include(q => q.Lignes)
            .Where(q => q.EntityId == _tenant.EntityId)
            .FirstOrDefaultAsync(q => q.Id == id)
            ?? throw new InvalidOperationException("Devis introuvable.");

        if (quote.Statut != QuoteStatus.Brouillon)
            throw new InvalidOperationException("Seuls les brouillons peuvent etre supprimes.");

        db.QuoteLines.RemoveRange(quote.Lignes);
        db.Quotes.Remove(quote);
        await db.SaveChangesAsync();
    }

    public async Task<Quote> ChangerStatutAsync(int id, QuoteStatus nouveauStatut)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        var quote = await db.Quotes.Include(q => q.Client).Include(q => q.Lignes)
            .Where(q => q.EntityId == _tenant.EntityId)
            .FirstOrDefaultAsync(q => q.Id == id)
            ?? throw new InvalidOperationException("Devis introuvable.");

        // Valider les transitions
        var transitionsValides = new Dictionary<QuoteStatus, QuoteStatus[]>
        {
            [QuoteStatus.Brouillon] = [QuoteStatus.Envoye],
            [QuoteStatus.Envoye] = [QuoteStatus.Accepte, QuoteStatus.Refuse, QuoteStatus.Expire]
        };

        if (!transitionsValides.TryGetValue(quote.Statut, out var cibles) || !cibles.Contains(nouveauStatut))
            throw new InvalidOperationException($"Transition {quote.Statut} \u2192 {nouveauStatut} non autoris\u00e9e.");

        quote.Statut = nouveauStatut;
        quote.UpdatedAt = DateTime.UtcNow;

        // Generer le PDF a l'envoi
        if (nouveauStatut == QuoteStatus.Envoye)
        {
            var settings = await db.Entities.FirstAsync(e => e.Id == _tenant.EntityId);
            var pdfPath = await _pdfService.GenererDevisPdfAsync(quote, settings);
            quote.CheminPdf = pdfPath;
        }

        await db.SaveChangesAsync();
        return quote;
    }

    public async Task<Invoice> ConvertirEnFactureAsync(int quoteId)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        var quote = await db.Quotes.Include(q => q.Client).Include(q => q.Lignes)
            .Where(q => q.EntityId == _tenant.EntityId)
            .FirstOrDefaultAsync(q => q.Id == quoteId)
            ?? throw new InvalidOperationException("Devis introuvable.");

        if (quote.Statut != QuoteStatus.Accepte)
            throw new InvalidOperationException("Seuls les devis accept\u00e9s peuvent \u00eatre convertis en facture.");

        if (quote.InvoiceId.HasValue)
            throw new InvalidOperationException("Ce devis a d\u00e9j\u00e0 \u00e9t\u00e9 converti en facture.");

        // Creer la facture en brouillon avec les lignes du devis
        var invoice = new Invoice
        {
            ClientId = quote.ClientId,
            DateEmission = DateTime.Today,
            DateEcheance = DateTime.Today.AddDays(30),
            Notes = quote.Notes,
            Lignes = quote.Lignes.Select(l => new InvoiceLine
            {
                Description = l.Description,
                Quantite = l.Quantite,
                PrixUnitaire = l.PrixUnitaire,
                TauxTVA = l.TauxTVA
            }).ToList()
        };

        var created = await _invoiceService.CreateAsync(invoice);

        // Lier le devis a la facture
        quote.InvoiceId = created.Id;
        quote.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return created;
    }

    private void CalculerTotaux(Quote quote, Entity settings)
    {
        if (settings.FranchiseTVA)
        {
            foreach (var ligne in quote.Lignes)
                ligne.TauxTVA = 0;
        }

        foreach (var ligne in quote.Lignes)
        {
            ligne.MontantHT = Math.Round(ligne.Quantite * ligne.PrixUnitaire, 2);
        }
        quote.MontantHT = quote.Lignes.Sum(l => l.MontantHT);
        quote.MontantTVA = quote.Lignes.Sum(l => Math.Round(l.MontantHT * l.TauxTVA / 100m, 2));
        quote.MontantTTC = quote.MontantHT + quote.MontantTVA;
    }
}
