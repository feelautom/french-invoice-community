using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using FrenchInvoice.Core.Data;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public class ImportService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ImportService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ImportService(IDbContextFactory<AppDbContext> factory, IWebHostEnvironment env, ILogger<ImportService> logger)
    {
        _factory = factory;
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Valide le ZIP et retourne les métadonnées sans importer.
    /// </summary>
    public async Task<ImportValidation> ValidateAsync(Stream zipStream)
    {
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);

        var metaEntry = archive.GetEntry("metadata.json");
        var dataEntry = archive.GetEntry("data.json");

        if (metaEntry == null || dataEntry == null)
            return new ImportValidation { IsValid = false, Error = "Archive invalide : metadata.json ou data.json manquant." };

        ExportMetadata metadata;
        using (var metaStream = metaEntry.Open())
        {
            metadata = await JsonSerializer.DeserializeAsync<ExportMetadata>(metaStream, JsonOptions)
                ?? throw new InvalidOperationException("metadata.json invalide.");
        }

        // Vérifier le hash d'intégrité
        byte[] dataBytes;
        using (var dataStream = dataEntry.Open())
        using (var ms = new MemoryStream())
        {
            await dataStream.CopyToAsync(ms);
            dataBytes = ms.ToArray();
        }

        var hash = Convert.ToHexString(SHA256.HashData(dataBytes)).ToLowerInvariant();
        if (hash != metadata.DataHashSha256)
            return new ImportValidation { IsValid = false, Error = "Intégrité compromise : le hash SHA-256 ne correspond pas." };

        var payload = JsonSerializer.Deserialize<ExportPayload>(dataBytes, JsonOptions);
        if (payload == null)
            return new ImportValidation { IsValid = false, Error = "data.json invalide." };

        return new ImportValidation
        {
            IsValid = true,
            Metadata = metadata,
            Payload = payload
        };
    }

    /// <summary>
    /// Importe les données dans une entité vide. L'entité cible doit exister et être vide.
    /// </summary>
    public async Task ImportAsync(int targetEntityId, Stream zipStream)
    {
        using var db = _factory.CreateDbContext();

        // Vérifier que l'entité cible est vide
        var hasData = await db.Revenues.AnyAsync(r => r.EntityId == targetEntityId)
            || await db.Expenses.AnyAsync(e => e.EntityId == targetEntityId)
            || await db.Invoices.AnyAsync(i => i.EntityId == targetEntityId);

        if (hasData)
            throw new InvalidOperationException("L'entité cible contient déjà des données. L'import n'est possible que sur une entité vide.");

        // Valider
        var validation = await ValidateAsync(zipStream);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.Error);

        var payload = validation.Payload!;

        // Rembobiner le stream pour extraire les fichiers
        zipStream.Seek(0, SeekOrigin.Begin);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);

        // Mapper les anciens IDs vers les nouveaux
        var clientIdMap = new Dictionary<int, int>();
        var revenueIdMap = new Dictionary<int, int>();

        // 1. Mettre à jour l'entité cible avec les paramètres importés
        var targetEntity = await db.Entities.FindAsync(targetEntityId)
            ?? throw new InvalidOperationException("Entité cible introuvable.");

        targetEntity.TypeActivite = payload.Entity.TypeActivite;
        targetEntity.PeriodiciteDeclaration = payload.Entity.PeriodiciteDeclaration;
        targetEntity.DateDebutActivite = payload.Entity.DateDebutActivite;
        targetEntity.PlafondCA = payload.Entity.PlafondCA;
        targetEntity.NumeroSiret = payload.Entity.NumeroSiret;
        targetEntity.TvaIntracommunautaire = payload.Entity.TvaIntracommunautaire;
        targetEntity.Telephone = payload.Entity.Telephone;
        targetEntity.Email = payload.Entity.Email;
        targetEntity.AdresseSiege = payload.Entity.AdresseSiege;
        targetEntity.CodePostal = payload.Entity.CodePostal;
        targetEntity.Ville = payload.Entity.Ville;
        targetEntity.CodePays = payload.Entity.CodePays;
        targetEntity.PrefixeFactures = payload.Entity.PrefixeFactures;
        targetEntity.ProchainNumeroFacture = payload.Entity.ProchainNumeroFacture;
        targetEntity.PrefixeDevis = payload.Entity.PrefixeDevis;
        targetEntity.ProchainNumeroDevis = payload.Entity.ProchainNumeroDevis;
        targetEntity.MentionsLegales = payload.Entity.MentionsLegales;
        targetEntity.FranchiseTVA = payload.Entity.FranchiseTVA;
        targetEntity.TauxTVA = payload.Entity.TauxTVA;
        targetEntity.VersementLiberatoire = payload.Entity.VersementLiberatoire;
        targetEntity.TauxLiberatoire = payload.Entity.TauxLiberatoire;
        targetEntity.FraisVariables = payload.Entity.FraisVariables;
        targetEntity.BeneficieACRE = payload.Entity.BeneficieACRE;
        targetEntity.Configured = true;
        targetEntity.UpdatedAt = DateTime.UtcNow;

        // 2. Clients
        foreach (var client in payload.Clients)
        {
            var oldId = client.Id;
            client.Id = 0;
            client.EntityId = targetEntityId;
            client.SiretDataId = null;
            client.SiretData = null;
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            clientIdMap[oldId] = client.Id;
        }

        // 3. Revenus
        foreach (var rev in payload.Revenues)
        {
            var oldId = rev.Id;
            rev.Id = 0;
            rev.EntityId = targetEntityId;
            db.Revenues.Add(rev);
            await db.SaveChangesAsync();
            revenueIdMap[oldId] = rev.Id;
        }

        // 4. Dépenses
        foreach (var exp in payload.Expenses)
        {
            exp.Id = 0;
            exp.EntityId = targetEntityId;
            db.Expenses.Add(exp);
        }
        await db.SaveChangesAsync();

        // 5. Factures + lignes
        foreach (var inv in payload.Invoices)
        {
            inv.Id = 0;
            inv.EntityId = targetEntityId;
            inv.ClientId = clientIdMap.GetValueOrDefault(inv.ClientId, 0);
            inv.RevenueId = inv.RevenueId.HasValue ? revenueIdMap.GetValueOrDefault(inv.RevenueId.Value) : null;
            inv.Client = null!;
            inv.Revenue = null;
            foreach (var line in inv.Lignes)
            {
                line.Id = 0;
                line.InvoiceId = 0;
                line.Invoice = null!;
            }
            db.Invoices.Add(inv);
        }
        await db.SaveChangesAsync();

        // 6. Devis + lignes
        foreach (var quote in payload.Quotes)
        {
            quote.Id = 0;
            quote.EntityId = targetEntityId;
            quote.ClientId = clientIdMap.GetValueOrDefault(quote.ClientId, 0);
            quote.InvoiceId = null;
            quote.Client = null!;
            quote.Invoice = null;
            foreach (var line in quote.Lignes)
            {
                line.Id = 0;
                line.QuoteId = 0;
                line.Quote = null!;
            }
            db.Quotes.Add(quote);
        }
        await db.SaveChangesAsync();

        // 7. Déclarations
        foreach (var decl in payload.Declarations)
        {
            decl.Id = 0;
            decl.EntityId = targetEntityId;
            db.Declarations.Add(decl);
        }

        // 8. Transactions bancaires
        foreach (var bt in payload.BankTransactions)
        {
            bt.Id = 0;
            bt.EntityId = targetEntityId;
            bt.RevenueId = bt.RevenueId.HasValue ? revenueIdMap.GetValueOrDefault(bt.RevenueId.Value) : null;
            bt.ExpenseId = null;
            bt.Revenue = null;
            bt.Expense = null;
            db.BankTransactions.Add(bt);
        }

        // 9. Charges fixes
        foreach (var fc in payload.FixedCharges)
        {
            fc.Id = 0;
            fc.EntityId = targetEntityId;
            db.FixedCharges.Add(fc);
        }

        // 10. Profils CSV
        foreach (var p in payload.CsvProfiles)
        {
            p.Id = 0;
            p.EntityId = targetEntityId;
            db.CsvMappingProfiles.Add(p);
        }

        await db.SaveChangesAsync();

        // 11. Extraire les fichiers du ZIP
        await ExtractFilesAsync(archive);

        _logger.LogInformation("Import terminé pour l'entité {EntityId}", targetEntityId);
    }

    private async Task ExtractFilesAsync(ZipArchive archive)
    {
        foreach (var entry in archive.Entries)
        {
            if (entry.Name == "" || entry.FullName == "data.json" || entry.FullName == "metadata.json")
                continue;

            string targetDir;
            if (entry.FullName.StartsWith("invoices/"))
                targetDir = Path.Combine(_env.ContentRootPath, "Data", "invoices");
            else if (entry.FullName.StartsWith("quotes/"))
                targetDir = Path.Combine(_env.ContentRootPath, "Data", "quotes");
            else if (entry.FullName.StartsWith("justificatifs/"))
                targetDir = Path.Combine(_env.ContentRootPath, "Data", "justificatifs",
                    entry.FullName.Contains("/recettes/") ? "recettes" :
                    entry.FullName.Contains("/achats/") ? "achats" : "declarations");
            else
                continue;

            Directory.CreateDirectory(targetDir);
            var filePath = Path.Combine(targetDir, entry.Name);
            using var entryStream = entry.Open();
            using var fileStream = new FileStream(filePath, FileMode.Create);
            await entryStream.CopyToAsync(fileStream);
        }
    }
}

public class ImportValidation
{
    public bool IsValid { get; set; }
    public string? Error { get; set; }
    public ExportMetadata? Metadata { get; set; }
    public ExportPayload? Payload { get; set; }
}
