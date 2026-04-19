using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using FrenchInvoice.Core.Data;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public class HashChainService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ILogger<HashChainService> _logger;

    public HashChainService(IDbContextFactory<AppDbContext> factory, ILogger<HashChainService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task RecordRevenueAsync(Revenue revenue)
    {
        await AppendEntryAsync(
            revenue.EntityId,
            AccountingEntryType.Recette,
            revenue.Date,
            revenue.Montant,
            revenue.Description,
            revenue.Client,
            revenueId: revenue.Id);
    }

    public async Task RecordExpenseAsync(Expense expense)
    {
        await AppendEntryAsync(
            expense.EntityId,
            AccountingEntryType.Depense,
            expense.Date,
            expense.Montant,
            expense.Description,
            expense.Fournisseur,
            expenseId: expense.Id);
    }

    private async Task AppendEntryAsync(
        int entityId,
        AccountingEntryType entryType,
        DateTime date,
        decimal montant,
        string description,
        string tiers,
        int? revenueId = null,
        int? expenseId = null)
    {
        using var db = _factory.CreateDbContext();
        using var tx = await db.Database.BeginTransactionAsync();

        var last = await db.AccountingEntries
            .Where(e => e.EntityId == entityId)
            .OrderByDescending(e => e.SequenceNumber)
            .FirstOrDefaultAsync();

        var seq = (last?.SequenceNumber ?? 0) + 1;
        var previousHash = last?.Hash ?? string.Empty;

        var entry = new AccountingEntry
        {
            EntityId = entityId,
            SequenceNumber = seq,
            EntryType = entryType,
            RevenueId = revenueId,
            ExpenseId = expenseId,
            Date = date,
            Montant = montant,
            Description = description,
            Tiers = tiers,
            PreviousHash = previousHash
        };

        entry.Hash = ComputeHash(entry);

        db.AccountingEntries.Add(entry);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        _logger.LogInformation("Écriture comptable #{Seq} chaînée pour entité {EntityId}", seq, entityId);
    }

    public static string ComputeHash(AccountingEntry entry)
    {
        var payload = string.Join("|",
            entry.SequenceNumber,
            entry.EntryType,
            entry.Date.ToString("yyyy-MM-dd"),
            entry.Montant.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            entry.Description,
            entry.Tiers,
            entry.PreviousHash);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public async Task<ChainVerificationResult> VerifyChainAsync(int entityId)
    {
        using var db = _factory.CreateDbContext();

        var entries = await db.AccountingEntries
            .Where(e => e.EntityId == entityId)
            .OrderBy(e => e.SequenceNumber)
            .ToListAsync();

        if (entries.Count == 0)
            return new ChainVerificationResult { IsValid = true, EntryCount = 0 };

        var previousHash = string.Empty;
        foreach (var entry in entries)
        {
            if (entry.PreviousHash != previousHash)
            {
                return new ChainVerificationResult
                {
                    IsValid = false,
                    EntryCount = entries.Count,
                    BrokenAtSequence = entry.SequenceNumber,
                    Error = $"PreviousHash incohérent à l'écriture #{entry.SequenceNumber}"
                };
            }

            var computed = ComputeHash(entry);
            if (entry.Hash != computed)
            {
                return new ChainVerificationResult
                {
                    IsValid = false,
                    EntryCount = entries.Count,
                    BrokenAtSequence = entry.SequenceNumber,
                    Error = $"Hash altéré à l'écriture #{entry.SequenceNumber}"
                };
            }

            previousHash = entry.Hash;
        }

        return new ChainVerificationResult { IsValid = true, EntryCount = entries.Count };
    }

    public async Task BackfillChainAsync(int entityId)
    {
        using var db = _factory.CreateDbContext();

        var existingCount = await db.AccountingEntries.CountAsync(e => e.EntityId == entityId);
        if (existingCount > 0)
        {
            _logger.LogInformation("Backfill ignoré : {Count} écritures existantes pour entité {EntityId}", existingCount, entityId);
            return;
        }

        var revenues = await db.Revenues
            .Where(r => r.EntityId == entityId)
            .OrderBy(r => r.Date).ThenBy(r => r.CreatedAt)
            .ToListAsync();

        var expenses = await db.Expenses
            .Where(e => e.EntityId == entityId)
            .OrderBy(e => e.Date).ThenBy(e => e.CreatedAt)
            .ToListAsync();

        var allEntries = revenues.Select(r => new
        {
            Date = r.Date,
            CreatedAt = r.CreatedAt,
            Type = AccountingEntryType.Recette,
            Montant = r.Montant,
            Description = r.Description,
            Tiers = r.Client,
            RevenueId = (int?)r.Id,
            ExpenseId = (int?)null
        })
        .Concat(expenses.Select(e => new
        {
            Date = e.Date,
            CreatedAt = e.CreatedAt,
            Type = AccountingEntryType.Depense,
            Montant = e.Montant,
            Description = e.Description,
            Tiers = e.Fournisseur,
            RevenueId = (int?)null,
            ExpenseId = (int?)e.Id
        }))
        .OrderBy(e => e.Date)
        .ThenBy(e => e.CreatedAt)
        .ToList();

        if (allEntries.Count == 0) return;

        using var tx = await db.Database.BeginTransactionAsync();

        var previousHash = string.Empty;
        long seq = 0;

        foreach (var item in allEntries)
        {
            seq++;
            var entry = new AccountingEntry
            {
                EntityId = entityId,
                SequenceNumber = seq,
                EntryType = item.Type,
                RevenueId = item.RevenueId,
                ExpenseId = item.ExpenseId,
                Date = item.Date,
                Montant = item.Montant,
                Description = item.Description,
                Tiers = item.Tiers,
                PreviousHash = previousHash
            };
            entry.Hash = ComputeHash(entry);
            previousHash = entry.Hash;

            db.AccountingEntries.Add(entry);
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        _logger.LogInformation("Backfill terminé : {Count} écritures chaînées pour entité {EntityId}", seq, entityId);
    }
}

public class ChainVerificationResult
{
    public bool IsValid { get; set; }
    public int EntryCount { get; set; }
    public long? BrokenAtSequence { get; set; }
    public string? Error { get; set; }
}
