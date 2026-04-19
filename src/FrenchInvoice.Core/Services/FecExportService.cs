using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using FrenchInvoice.Core.Data;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public class FecExportService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ILogger<FecExportService> _logger;

    public FecExportService(IDbContextFactory<AppDbContext> factory, ILogger<FecExportService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<(byte[] Data, string FileName)> GenerateFecAsync(int entityId, int year)
    {
        var start = new DateTime(year, 1, 1);
        var end = new DateTime(year, 12, 31);
        return await GenerateFecAsync(entityId, start, end);
    }

    public async Task<(byte[] Data, string FileName)> GenerateFecAsync(int entityId, DateTime start, DateTime end)
    {
        using var db = _factory.CreateDbContext();

        var entity = await db.Entities
            .Include(e => e.SiretData)
            .FirstOrDefaultAsync(e => e.Id == entityId)
            ?? throw new InvalidOperationException("Entité introuvable.");

        var entries = await db.AccountingEntries
            .Where(e => e.EntityId == entityId && e.Date >= start && e.Date <= end)
            .OrderBy(e => e.SequenceNumber)
            .ToListAsync();

        // Fallback si pas d'écritures chaînées : générer depuis Revenue/Expense
        if (entries.Count == 0)
        {
            entries = await BuildEntriesFromRawDataAsync(db, entityId, start, end);
        }

        var siren = entity.SiretData?.Siren
            ?? (entity.NumeroSiret.Length >= 9 ? entity.NumeroSiret[..9] : "000000000");
        var fileName = $"{siren}FEC{end:yyyyMMdd}.txt";

        // ISO 8859-15 requis par la DGFiP
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding("iso-8859-15");

        var sb = new StringBuilder();

        // En-têtes
        sb.AppendLine(string.Join("\t",
            "JournalCode", "JournalLib", "EcritureNum", "EcritureDate",
            "CompteNum", "CompteLib", "CompAuxNum", "CompAuxLib",
            "PieceRef", "PieceDate", "EcritureLib",
            "Debit", "Credit", "EcrtureLet", "DateLet",
            "ValidDate", "Montantdevise", "Idevise"));

        foreach (var entry in entries)
        {
            var isRecette = entry.EntryType == AccountingEntryType.Recette;
            var journalCode = isRecette ? "VE" : "AC";
            var journalLib = isRecette ? "Journal des ventes" : "Journal des achats";
            var compteNum = isRecette ? "706000" : "607000";
            var compteLib = isRecette ? "Prestations de services" : "Achats";
            var debit = isRecette ? "0,00" : entry.Montant.ToString("F2", CultureInfo.GetCultureInfo("fr-FR"));
            var credit = isRecette ? entry.Montant.ToString("F2", CultureInfo.GetCultureInfo("fr-FR")) : "0,00";

            var pieceRef = entry.RevenueId.HasValue
                ? (await db.Revenues.FindAsync(entry.RevenueId.Value))?.ReferenceFacture ?? $"REC-{entry.RevenueId}"
                : $"DEP-{entry.ExpenseId}";

            sb.AppendLine(string.Join("\t",
                journalCode,
                journalLib,
                entry.SequenceNumber.ToString(),
                entry.Date.ToString("yyyyMMdd"),
                compteNum,
                compteLib,
                entry.Tiers,
                entry.Tiers,
                pieceRef,
                entry.Date.ToString("yyyyMMdd"),
                entry.Description,
                debit,
                credit,
                "", // EcrtureLet
                "", // DateLet
                entry.CreatedAt.ToString("yyyyMMdd"),
                "", // Montantdevise
                "EUR"));
        }

        var bytes = encoding.GetBytes(sb.ToString());
        _logger.LogInformation("FEC généré pour entité {EntityId} : {Count} écritures, {Size} octets", entityId, entries.Count, bytes.Length);

        return (bytes, fileName);
    }

    private async Task<List<AccountingEntry>> BuildEntriesFromRawDataAsync(AppDbContext db, int entityId, DateTime start, DateTime end)
    {
        var revenues = await db.Revenues
            .Where(r => r.EntityId == entityId && r.Date >= start && r.Date <= end)
            .OrderBy(r => r.Date).ThenBy(r => r.CreatedAt)
            .ToListAsync();

        var expenses = await db.Expenses
            .Where(e => e.EntityId == entityId && e.Date >= start && e.Date <= end)
            .OrderBy(e => e.Date).ThenBy(e => e.CreatedAt)
            .ToListAsync();

        long seq = 0;
        var entries = new List<AccountingEntry>();

        var all = revenues.Select(r => new { Date = r.Date, CreatedAt = r.CreatedAt, IsRevenue = true, Rev = (Revenue?)r, Exp = (Expense?)null })
            .Concat(expenses.Select(e => new { Date = e.Date, CreatedAt = e.CreatedAt, IsRevenue = false, Rev = (Revenue?)null, Exp = (Expense?)e }))
            .OrderBy(x => x.Date).ThenBy(x => x.CreatedAt);

        foreach (var item in all)
        {
            seq++;
            entries.Add(new AccountingEntry
            {
                EntityId = entityId,
                SequenceNumber = seq,
                EntryType = item.IsRevenue ? AccountingEntryType.Recette : AccountingEntryType.Depense,
                RevenueId = item.Rev?.Id,
                ExpenseId = item.Exp?.Id,
                Date = item.Date,
                Montant = item.IsRevenue ? item.Rev!.Montant : item.Exp!.Montant,
                Description = item.IsRevenue ? item.Rev!.Description : item.Exp!.Description,
                Tiers = item.IsRevenue ? item.Rev!.Client : item.Exp!.Fournisseur,
                CreatedAt = item.CreatedAt
            });
        }

        _logger.LogWarning("FEC généré en fallback (pas d'écritures chaînées) pour entité {EntityId}", entityId);
        return entries;
    }
}
