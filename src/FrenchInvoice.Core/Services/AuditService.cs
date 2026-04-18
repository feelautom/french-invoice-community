using Microsoft.EntityFrameworkCore;
using FrenchInvoice.Core.Data;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public class AuditService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IDbContextFactory<AppDbContext> factory, ILogger<AuditService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task LogAsync(string action, int? entityId = null, int? userId = null,
        string? username = null, string? detail = null, string? ipAddress = null)
    {
        try
        {
            using var db = _factory.CreateDbContext();
            db.AuditLogs.Add(new AuditLog
            {
                EntityId = entityId,
                UserId = userId,
                Username = username ?? "",
                Action = action,
                Detail = detail,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Ne jamais bloquer l'opération principale pour un log d'audit
            _logger.LogWarning(ex, "Échec de l'écriture du log d'audit : {Action}", action);
        }
    }

    public async Task<List<AuditLog>> GetLogsAsync(int entityId, int limit = 100)
    {
        using var db = _factory.CreateDbContext();
        return await db.AuditLogs
            .Where(a => a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetAllLogsAsync(int limit = 200)
    {
        using var db = _factory.CreateDbContext();
        return await db.AuditLogs
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}
