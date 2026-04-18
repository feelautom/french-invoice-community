using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using FrenchInvoice.Core.Data;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public class ExportService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ExportService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExportService(IDbContextFactory<AppDbContext> factory, IWebHostEnvironment env, ILogger<ExportService> logger)
    {
        _factory = factory;
        _env = env;
        _logger = logger;
    }

    public async Task<byte[]> ExportEntityAsync(int entityId)
    {
        using var db = _factory.CreateDbContext();

        var entity = await db.Entities.FindAsync(entityId)
            ?? throw new InvalidOperationException("Entité introuvable.");

        var revenues = await db.Revenues.Where(r => r.EntityId == entityId).OrderBy(r => r.Date).ToListAsync();
        var expenses = await db.Expenses.Where(e => e.EntityId == entityId).OrderBy(e => e.Date).ToListAsync();
        var clients = await db.Clients.Where(c => c.EntityId == entityId).ToListAsync();
        var invoices = await db.Invoices.Include(i => i.Lignes).Where(i => i.EntityId == entityId).ToListAsync();
        var quotes = await db.Quotes.Include(q => q.Lignes).Where(q => q.EntityId == entityId).ToListAsync();
        var declarations = await db.Declarations.Where(d => d.EntityId == entityId).ToListAsync();
        var bankTransactions = await db.BankTransactions.Where(bt => bt.EntityId == entityId).ToListAsync();
        var fixedCharges = await db.FixedCharges.Where(fc => fc.EntityId == entityId).ToListAsync();
        var payoutRecords = await db.PayoutRecords.Where(pr => pr.EntityId == entityId).ToListAsync();
        var csvProfiles = await db.CsvMappingProfiles.Where(p => p.EntityId == entityId).ToListAsync();

        var exportData = new ExportPayload
        {
            SchemaVersion = 1,
            ExportDate = DateTime.UtcNow,
            EntityId = entityId,
            Entity = entity,
            Revenues = revenues,
            Expenses = expenses,
            Clients = clients,
            Invoices = invoices,
            Quotes = quotes,
            Declarations = declarations,
            BankTransactions = bankTransactions,
            FixedCharges = fixedCharges,
            PayoutRecords = payoutRecords,
            CsvProfiles = csvProfiles
        };

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var jsonEntry = archive.CreateEntry("data.json", CompressionLevel.Optimal);
            using (var jsonStream = jsonEntry.Open())
            {
                await JsonSerializer.SerializeAsync(jsonStream, exportData, JsonOptions);
            }

            var invoiceDir = Path.Combine(_env.ContentRootPath, "Data", "invoices");
            foreach (var invoice in invoices.Where(i => !string.IsNullOrEmpty(i.Numero)))
            {
                var pdfPath = Path.Combine(invoiceDir, $"{invoice.Numero}.pdf");
                if (File.Exists(pdfPath))
                    archive.CreateEntryFromFile(pdfPath, $"invoices/{invoice.Numero}.pdf", CompressionLevel.Optimal);
            }

            var quoteDir = Path.Combine(_env.ContentRootPath, "Data", "quotes");
            foreach (var quote in quotes.Where(q => !string.IsNullOrEmpty(q.Numero)))
            {
                var pdfPath = Path.Combine(quoteDir, $"{quote.Numero}.pdf");
                if (File.Exists(pdfPath))
                    archive.CreateEntryFromFile(pdfPath, $"quotes/{quote.Numero}.pdf", CompressionLevel.Optimal);
            }

            var recettesDir = Path.Combine(_env.ContentRootPath, "Data", "justificatifs", "recettes");
            foreach (var rev in revenues.Where(r => !string.IsNullOrEmpty(r.JustificatifFileName)))
            {
                var filePath = Path.Combine(recettesDir, rev.JustificatifFileName!);
                if (File.Exists(filePath))
                    archive.CreateEntryFromFile(filePath, $"justificatifs/recettes/{rev.JustificatifFileName}", CompressionLevel.Optimal);
            }

            var achatsDir = Path.Combine(_env.ContentRootPath, "Data", "justificatifs", "achats");
            foreach (var exp in expenses.Where(e => !string.IsNullOrEmpty(e.Justificatif)))
            {
                var filePath = Path.Combine(achatsDir, exp.Justificatif!);
                if (File.Exists(filePath))
                    archive.CreateEntryFromFile(filePath, $"justificatifs/achats/{exp.Justificatif}", CompressionLevel.Optimal);
            }

            var declDir = Path.Combine(_env.ContentRootPath, "Data", "justificatifs", "declarations");
            foreach (var decl in declarations.Where(d => !string.IsNullOrEmpty(d.JustificatifFileName)))
            {
                var filePath = Path.Combine(declDir, decl.JustificatifFileName!);
                if (File.Exists(filePath))
                    archive.CreateEntryFromFile(filePath, $"justificatifs/declarations/{decl.JustificatifFileName}", CompressionLevel.Optimal);
            }

            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(exportData, JsonOptions);
            var hash = SHA256.HashData(jsonBytes);
            var metadata = new ExportMetadata
            {
                SchemaVersion = 1,
                ExportDate = exportData.ExportDate,
                EntityId = entityId,
                EntityName = entity.Nom,
                DataHashSha256 = Convert.ToHexString(hash).ToLowerInvariant(),
                RevenueCount = revenues.Count,
                ExpenseCount = expenses.Count,
                InvoiceCount = invoices.Count,
                QuoteCount = quotes.Count,
                ClientCount = clients.Count
            };

            var metaEntry = archive.CreateEntry("metadata.json", CompressionLevel.Optimal);
            using (var metaStream = metaEntry.Open())
            {
                await JsonSerializer.SerializeAsync(metaStream, metadata, JsonOptions);
            }
        }

        _logger.LogInformation("Export entité {EntityId} : {Size} octets", entityId, memoryStream.Length);
        return memoryStream.ToArray();
    }
}

public class ExportPayload
{
    public int SchemaVersion { get; set; }
    public DateTime ExportDate { get; set; }
    public int EntityId { get; set; }
    public Entity Entity { get; set; } = null!;
    public List<Revenue> Revenues { get; set; } = new();
    public List<Expense> Expenses { get; set; } = new();
    public List<Client> Clients { get; set; } = new();
    public List<Invoice> Invoices { get; set; } = new();
    public List<Quote> Quotes { get; set; } = new();
    public List<Declaration> Declarations { get; set; } = new();
    public List<BankTransaction> BankTransactions { get; set; } = new();
    public List<FixedCharge> FixedCharges { get; set; } = new();
    public List<PayoutRecord> PayoutRecords { get; set; } = new();
    public List<CsvMappingProfile> CsvProfiles { get; set; } = new();
}

public class ExportMetadata
{
    public int SchemaVersion { get; set; }
    public DateTime ExportDate { get; set; }
    public int EntityId { get; set; }
    public string EntityName { get; set; } = "";
    public string DataHashSha256 { get; set; } = "";
    public int RevenueCount { get; set; }
    public int ExpenseCount { get; set; }
    public int InvoiceCount { get; set; }
    public int QuoteCount { get; set; }
    public int ClientCount { get; set; }
}
