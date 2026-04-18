using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using FrenchInvoice.Core.Data;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Tests.Fixtures;

public class DatabaseFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public DatabaseFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var db = CreateDbContext();
        db.Database.EnsureCreated();
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new AppDbContext(options);
    }

    public IDbContextFactory<AppDbContext> CreateFactory()
    {
        return new TestDbContextFactory(_connection);
    }

    public Entity SeedEntity(AppDbContext db, string nom = "Test Entity",
        ActivityCategory type = ActivityCategory.BNC, bool acre = false)
    {
        var entity = new Entity
        {
            Nom = nom,
            TypeActivite = type,
            PeriodiciteDeclaration = DeclarationPeriodicity.Mensuelle,
            DateDebutActivite = new DateTime(2026, 1, 1),
            PlafondCA = 77700m,
            NumeroSiret = "12345678901234",
            FranchiseTVA = true,
            TauxTVA = 20m,
            BeneficieACRE = acre,
            PrefixeFactures = "FAC-2026-",
            ProchainNumeroFacture = 1,
            PrefixeDevis = "DEV-2026-",
            ProchainNumeroDevis = 1,
            Configured = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Entities.Add(entity);
        db.SaveChanges();
        return entity;
    }

    public Client SeedClient(AppDbContext db, int entityId, string nom = "Client Test")
    {
        var client = new Client
        {
            EntityId = entityId,
            Nom = nom,
            Email = "client@test.fr",
            Adresse = "1 rue de Test",
            CodePostal = "75001",
            Ville = "Paris",
            Pays = "France"
        };
        db.Clients.Add(client);
        db.SaveChanges();
        return client;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly SqliteConnection _connection;

        public TestDbContextFactory(SqliteConnection connection) => _connection = connection;

        public AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;
            return new AppDbContext(options);
        }
    }
}
