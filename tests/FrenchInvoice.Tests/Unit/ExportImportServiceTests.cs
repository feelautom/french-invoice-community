using System.IO.Compression;
using System.Text.Json;
using FluentAssertions;
using FrenchInvoice.Core.Data;
using FrenchInvoice.Core.Models;
using FrenchInvoice.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FrenchInvoice.Tests.Unit;

public class ExportImportServiceTests : IDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly string _tempDir;

    public ExportImportServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "fi_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(Path.Combine(_tempDir, "Data"));

        _connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();

        _factory = new TestDbContextFactory(_options);
    }

    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private AppDbContext CreateDb() => new AppDbContext(_options);

    private async Task<(int entityId, ExportService exportService)> SeedAndCreateService()
    {
        using var db = CreateDb();
        var entity = new Entity
        {
            Nom = "Test Export",
            NumeroSiret = "12345678901234",
            TypeActivite = ActivityCategory.BNC,
            FranchiseTVA = true,
            Configured = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Entities.Add(entity);
        await db.SaveChangesAsync();

        db.Revenues.Add(new Revenue
        {
            EntityId = entity.Id,
            Date = new DateTime(2026, 1, 15),
            Montant = 1500m,
            Description = "Prestation",
            Client = "Client A",
            ModePaiement = "Virement",
            Categorie = ActivityCategory.BNC
        });

        db.Expenses.Add(new Expense
        {
            EntityId = entity.Id,
            Date = new DateTime(2026, 1, 20),
            Montant = 50m,
            Description = "Logiciel",
            Fournisseur = "Adobe",
            Categorie = "Logiciel / SaaS",
            ModeReglement = "Carte bancaire"
        });

        var client = new Client
        {
            EntityId = entity.Id,
            Nom = "Client A",
            Email = "a@test.fr"
        };
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var env = new TestWebHostEnvironment(_tempDir);
        var service = new ExportService(_factory, env, NullLogger<ExportService>.Instance);
        return (entity.Id, service);
    }

    [Fact]
    public async Task Export_ReturnsValidZip()
    {
        var (entityId, service) = await SeedAndCreateService();

        var bytes = await service.ExportEntityAsync(entityId);

        bytes.Should().NotBeEmpty();

        using var ms = new MemoryStream(bytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        archive.GetEntry("data.json").Should().NotBeNull();
        archive.GetEntry("metadata.json").Should().NotBeNull();
    }

    [Fact]
    public async Task Export_MetadataContainsCorrectCounts()
    {
        var (entityId, service) = await SeedAndCreateService();

        var bytes = await service.ExportEntityAsync(entityId);

        using var ms = new MemoryStream(bytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        var metaEntry = archive.GetEntry("metadata.json")!;
        using var stream = metaEntry.Open();
        var metadata = await JsonSerializer.DeserializeAsync<ExportMetadata>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        metadata.Should().NotBeNull();
        metadata!.RevenueCount.Should().Be(1);
        metadata.ExpenseCount.Should().Be(1);
        metadata.ClientCount.Should().Be(1);
        metadata.EntityName.Should().Be("Test Export");
        metadata.DataHashSha256.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Export_ThenValidate_IsValid()
    {
        var (entityId, exportService) = await SeedAndCreateService();
        var env = new TestWebHostEnvironment(_tempDir);
        var importService = new ImportService(_factory, env, NullLogger<ImportService>.Instance);

        var bytes = await exportService.ExportEntityAsync(entityId);

        using var ms = new MemoryStream(bytes);
        var validation = await importService.ValidateAsync(ms);

        validation.IsValid.Should().BeTrue();
        validation.Payload.Should().NotBeNull();
        validation.Payload!.Revenues.Should().HaveCount(1);
        validation.Payload.Expenses.Should().HaveCount(1);
    }

    [Fact]
    public async Task Validate_InvalidZip_ReturnsError()
    {
        var env = new TestWebHostEnvironment(_tempDir);
        var importService = new ImportService(_factory, env, NullLogger<ImportService>.Instance);

        // ZIP vide sans data.json
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            archive.CreateEntry("dummy.txt");
        }
        ms.Seek(0, SeekOrigin.Begin);

        var validation = await importService.ValidateAsync(ms);
        validation.IsValid.Should().BeFalse();
        validation.Error.Should().Contain("metadata.json ou data.json manquant");
    }

    // Helper classes
    private class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;
        public AppDbContext CreateDbContext() => new AppDbContext(_options);
    }

    private class TestWebHostEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public TestWebHostEnvironment(string contentRoot) => ContentRootPath = contentRoot;
        public string ContentRootPath { get; set; }
        public string WebRootPath { get; set; } = "";
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "Test";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
    }
}
