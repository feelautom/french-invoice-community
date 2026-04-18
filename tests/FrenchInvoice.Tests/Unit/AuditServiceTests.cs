using FluentAssertions;
using FrenchInvoice.Core.Data;
using FrenchInvoice.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FrenchInvoice.Tests.Unit;

public class AuditServiceTests : IDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly IDbContextFactory<AppDbContext> _factory;

    public AuditServiceTests()
    {
        _connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();
        _factory = new TestDbContextFactory(_options);
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task LogAsync_CreatesEntry()
    {
        var service = new AuditService(_factory, NullLogger<AuditService>.Instance);

        await service.LogAsync("LOGIN_SUCCESS", entityId: 1, userId: 1, username: "admin", detail: "Test login");

        using var db = new AppDbContext(_options);
        var logs = await db.AuditLogs.ToListAsync();
        logs.Should().HaveCount(1);
        logs[0].Action.Should().Be("LOGIN_SUCCESS");
        logs[0].Username.Should().Be("admin");
        logs[0].EntityId.Should().Be(1);
    }

    [Fact]
    public async Task GetLogsAsync_ReturnsOrderedByDate()
    {
        var service = new AuditService(_factory, NullLogger<AuditService>.Instance);

        await service.LogAsync("ACTION_1", entityId: 1, username: "a");
        await service.LogAsync("ACTION_2", entityId: 1, username: "b");
        await service.LogAsync("ACTION_3", entityId: 1, username: "c");

        var logs = await service.GetLogsAsync(1);
        logs.Should().HaveCount(3);
        logs[0].Action.Should().Be("ACTION_3"); // Plus récent en premier
    }

    [Fact]
    public async Task GetLogsAsync_FiltersbyEntity()
    {
        var service = new AuditService(_factory, NullLogger<AuditService>.Instance);

        await service.LogAsync("ACTION_1", entityId: 1);
        await service.LogAsync("ACTION_2", entityId: 2);

        var logs = await service.GetLogsAsync(1);
        logs.Should().HaveCount(1);
        logs[0].Action.Should().Be("ACTION_1");
    }

    [Fact]
    public async Task LogAsync_DoesNotThrowOnError()
    {
        // Utiliser un factory qui va créer un contexte avec une connexion fermée
        var badConnection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        // Ne pas ouvrir la connexion → les opérations échoueront
        var badOptions = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(badConnection).Options;
        var badFactory = new TestDbContextFactory(badOptions);

        var service = new AuditService(badFactory, NullLogger<AuditService>.Instance);

        // Ne doit PAS lever d'exception
        var action = () => service.LogAsync("TEST", entityId: 1);
        await action.Should().NotThrowAsync();
    }

    private class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;
        public AppDbContext CreateDbContext() => new AppDbContext(_options);
    }
}
