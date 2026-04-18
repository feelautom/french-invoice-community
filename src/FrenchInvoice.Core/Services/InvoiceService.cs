using Microsoft.EntityFrameworkCore;
using FrenchInvoice.Core.Data;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public class InvoiceService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly PdfGenerationService _pdfService;
    private readonly ITenantProvider _tenant;

    public InvoiceService(IDbContextFactory<AppDbContext> factory, PdfGenerationService pdfService,
        ITenantProvider tenant)
    {
        _factory = factory;
        _pdfService = pdfService;
        _tenant = tenant;
    }

    public async Task<List<Invoice>> GetAllAsync(InvoiceStatus? statut = null, int? clientId = null,
        DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        var query = db.Invoices.Include(i => i.Client).Include(i => i.Lignes)
            .Where(i => i.EntityId == _tenant.EntityId)
            .AsQueryable();

        if (statut.HasValue)
            query = query.Where(i => i.Statut == statut.Value);
        if (clientId.HasValue)
            query = query.Where(i => i.ClientId == clientId.Value);
        if (dateFrom.HasValue)
            query = query.Where(i => i.DateEmission >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(i => i.DateEmission <= dateTo.Value);

        return await query.OrderByDescending(i => i.DateEmission).ThenByDescending(i => i.Id).ToListAsync();
    }

    public async Task<Invoice?> GetByIdAsync(int id)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        return await db.Invoices.Include(i => i.Client).Include(i => i.Lignes)
            .Where(i => i.EntityId == _tenant.EntityId)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Invoice> CreateAsync(Invoice invoice)
    {
        await _tenant.InitializeAsync();
        CalculerTotaux(invoice);
        invoice.EntityId = _tenant.EntityId;
        invoice.Statut = InvoiceStatus.Brouillon;
        invoice.Numero = string.Empty;
        invoice.CreatedAt = DateTime.UtcNow;
        invoice.UpdatedAt = DateTime.UtcNow;

        using var db = _factory.CreateDbContext();
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        return await GetByIdAsync(invoice.Id) ?? invoice;
    }

    public async Task<Invoice> UpdateAsync(Invoice invoice)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        var existing = await db.Invoices.Include(i => i.Lignes)
            .Where(i => i.EntityId == _tenant.EntityId)
            .FirstOrDefaultAsync(i => i.Id == invoice.Id)
            ?? throw new InvalidOperationException("Facture introuvable.");

        if (existing.Statut != InvoiceStatus.Brouillon)
            throw new InvalidOperationException("Seuls les brouillons peuvent être modifiés.");

        existing.ClientId = invoice.ClientId;
        existing.DateEmission = invoice.DateEmission;
        existing.DateEcheance = invoice.DateEcheance;
        existing.Notes = invoice.Notes;
        existing.MentionsLegales = invoice.MentionsLegales;

        db.InvoiceLines.RemoveRange(existing.Lignes);
        existing.Lignes = invoice.Lignes.Select(l => new InvoiceLine
        {
            Description = l.Description,
            Quantite = l.Quantite,
            PrixUnitaire = l.PrixUnitaire,
            TauxTVA = l.TauxTVA
        }).ToList();

        CalculerTotaux(existing);
        existing.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await GetByIdAsync(existing.Id) ?? existing;
    }

    public async Task DeleteAsync(int id)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        var invoice = await db.Invoices.Include(i => i.Lignes)
            .Where(i => i.EntityId == _tenant.EntityId)
            .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new InvalidOperationException("Facture introuvable.");

        if (invoice.Statut != InvoiceStatus.Brouillon || !string.IsNullOrEmpty(invoice.Numero))
            throw new InvalidOperationException("Seuls les brouillons sans numéro peuvent être supprimés.");

        db.InvoiceLines.RemoveRange(invoice.Lignes);
        db.Invoices.Remove(invoice);
        await db.SaveChangesAsync();
    }

    public async Task<Invoice> FinaliserAsync(int id)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        using var transaction = await db.Database.BeginTransactionAsync();

        var invoice = await db.Invoices.Include(i => i.Client).Include(i => i.Lignes)
            .Where(i => i.EntityId == _tenant.EntityId)
            .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new InvalidOperationException("Facture introuvable.");

        if (invoice.Statut != InvoiceStatus.Brouillon)
            throw new InvalidOperationException("Seuls les brouillons peuvent être finalisés.");

        if (!invoice.Lignes.Any())
            throw new InvalidOperationException("La facture doit contenir au moins une ligne.");

        var settings = await db.Entities.FirstAsync(e => e.Id == _tenant.EntityId);

        // Attribution du numéro séquentiel (transaction pour atomicité)
        var numero = $"{settings.PrefixeFactures}{settings.ProchainNumeroFacture:D4}";
        settings.ProchainNumeroFacture++;

        // Appliquer franchise TVA : forcer les taux à 0 et ajouter les mentions légales
        if (settings.FranchiseTVA)
        {
            foreach (var ligne in invoice.Lignes)
                ligne.TauxTVA = 0m;
            invoice.MentionsLegales = settings.MentionsLegales;
        }

        CalculerTotaux(invoice);
        invoice.Numero = numero;
        invoice.Statut = InvoiceStatus.Envoyee;
        invoice.UpdatedAt = DateTime.UtcNow;

        // Sauvegarder numéro + statut + lignes TVA avant la génération PDF
        await db.SaveChangesAsync();

        // Générer le PDF Factur-X (après SaveChanges pour garantir la cohérence)
        var pdfPath = await _pdfService.GenererFacturePdfAsync(invoice, settings);
        invoice.CheminPdf = pdfPath;
        await db.SaveChangesAsync();

        await transaction.CommitAsync();

        return invoice;
    }

    public async Task<Invoice> MarquerPayeeAsync(int id, string? modePaiement = null)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        var invoice = await db.Invoices.Include(i => i.Client).Include(i => i.Lignes)
            .Where(i => i.EntityId == _tenant.EntityId)
            .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new InvalidOperationException("Facture introuvable.");

        if (invoice.Statut != InvoiceStatus.Envoyee)
            throw new InvalidOperationException("Seules les factures envoyées peuvent être marquées comme payées.");

        var settings = await db.Entities.FirstAsync(e => e.Id == _tenant.EntityId);

        // Créer la recette automatiquement
        var revenue = new Revenue
        {
            EntityId = _tenant.EntityId,
            Date = DateTime.Today,
            Montant = invoice.MontantTTC,
            Description = $"Facture {invoice.Numero}",
            Client = invoice.Client.Nom,
            ModePaiement = modePaiement ?? "Virement",
            Categorie = settings.TypeActivite,
            ReferenceFacture = invoice.Numero,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Revenues.Add(revenue);
        await db.SaveChangesAsync();

        invoice.Statut = InvoiceStatus.Payee;
        invoice.RevenueId = revenue.Id;
        invoice.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return invoice;
    }

    public async Task<Invoice> AnnulerAsync(int id)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        var invoice = await db.Invoices
            .Where(i => i.EntityId == _tenant.EntityId)
            .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new InvalidOperationException("Facture introuvable.");

        if (invoice.Statut != InvoiceStatus.Envoyee)
            throw new InvalidOperationException("Seules les factures envoyées peuvent être annulées.");

        invoice.Statut = InvoiceStatus.Annulee;
        invoice.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return invoice;
    }

    public void CalculerTotaux(Invoice invoice)
    {
        foreach (var ligne in invoice.Lignes)
        {
            ligne.MontantHT = Math.Round(ligne.Quantite * ligne.PrixUnitaire, 2);
        }
        invoice.MontantHT = invoice.Lignes.Sum(l => l.MontantHT);
        invoice.MontantTVA = invoice.Lignes.Sum(l => Math.Round(l.MontantHT * l.TauxTVA / 100m, 2));
        invoice.MontantTTC = invoice.MontantHT + invoice.MontantTVA;
    }
}
