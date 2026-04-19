using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using FrenchInvoice.Core.Data;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public class ClosingService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ILogger<ClosingService> _logger;

    public ClosingService(IDbContextFactory<AppDbContext> factory, ILogger<ClosingService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<DateTime?> GetLastClosingDateAsync(int entityId)
    {
        using var db = _factory.CreateDbContext();
        return await db.AccountingPeriodClosings
            .Where(c => c.EntityId == entityId)
            .OrderByDescending(c => c.PeriodEnd)
            .Select(c => (DateTime?)c.PeriodEnd)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsDateLockedAsync(int entityId, DateTime date)
    {
        var lastClosing = await GetLastClosingDateAsync(entityId);
        return lastClosing.HasValue && date.Date <= lastClosing.Value.Date;
    }

    public async Task<AccountingPeriodClosing> ClosePeriodAsync(int entityId, DateTime periodEnd, string username)
    {
        using var db = _factory.CreateDbContext();

        var lastClosing = await db.AccountingPeriodClosings
            .Where(c => c.EntityId == entityId)
            .OrderByDescending(c => c.PeriodEnd)
            .FirstOrDefaultAsync();

        if (lastClosing != null && periodEnd <= lastClosing.PeriodEnd)
            throw new InvalidOperationException($"La date de clôture doit être postérieure au {lastClosing.PeriodEnd:dd/MM/yyyy}.");

        if (periodEnd.Date >= DateTime.UtcNow.Date)
            throw new InvalidOperationException("La date de clôture doit être dans le passé.");

        var periodStart = lastClosing?.PeriodEnd.AddDays(1) ?? DateTime.MinValue;

        var entries = await db.AccountingEntries
            .Where(e => e.EntityId == entityId && e.Date <= periodEnd)
            .OrderBy(e => e.SequenceNumber)
            .ToListAsync();

        var totalRecettes = entries.Where(e => e.EntryType == AccountingEntryType.Recette).Sum(e => e.Montant);
        var totalDepenses = entries.Where(e => e.EntryType == AccountingEntryType.Depense).Sum(e => e.Montant);

        var hashPayload = string.Join("|", entries.Select(e => e.Hash));
        var sealHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hashPayload))).ToLowerInvariant();

        var closing = new AccountingPeriodClosing
        {
            EntityId = entityId,
            PeriodEnd = periodEnd,
            ClosedBy = username,
            Hash = sealHash,
            EntryCount = entries.Count,
            TotalRecettes = totalRecettes,
            TotalDepenses = totalDepenses
        };

        db.AccountingPeriodClosings.Add(closing);
        await db.SaveChangesAsync();

        _logger.LogInformation("Clôture comptable {PeriodEnd:yyyy-MM-dd} pour entité {EntityId} ({Count} écritures)", periodEnd, entityId, entries.Count);

        return closing;
    }

    public async Task<List<AccountingPeriodClosing>> GetClosingsAsync(int entityId)
    {
        using var db = _factory.CreateDbContext();
        return await db.AccountingPeriodClosings
            .Where(c => c.EntityId == entityId)
            .OrderByDescending(c => c.PeriodEnd)
            .ToListAsync();
    }

    public async Task<bool> VerifyClosingAsync(int entityId, int closingId)
    {
        using var db = _factory.CreateDbContext();

        var closing = await db.AccountingPeriodClosings.FindAsync(closingId);
        if (closing == null || closing.EntityId != entityId)
            return false;

        var entries = await db.AccountingEntries
            .Where(e => e.EntityId == entityId && e.Date <= closing.PeriodEnd)
            .OrderBy(e => e.SequenceNumber)
            .ToListAsync();

        var hashPayload = string.Join("|", entries.Select(e => e.Hash));
        var computed = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hashPayload))).ToLowerInvariant();

        return computed == closing.Hash;
    }
}
