using Microsoft.EntityFrameworkCore;
using FrenchInvoice.Core.Data;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public class AlertService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ITenantProvider _tenant;

    public AlertService(IDbContextFactory<AppDbContext> factory, ITenantProvider tenant)
    {
        _factory = factory;
        _tenant = tenant;
    }

    public async Task<List<AlertInfo>> GetActiveAlertsAsync()
    {
        await _tenant.InitializeAsync();
        var alerts = new List<AlertInfo>();
        using var db = _factory.CreateDbContext();

        var upcoming = await db.Declarations
            .Where(d => d.EntityId == _tenant.EntityId)
            .Where(d => d.Statut == DeclarationStatut.AFaire && d.DateLimite >= DateTime.Today)
            .OrderBy(d => d.DateLimite)
            .ToListAsync();

        foreach (var decl in upcoming)
        {
            var daysLeft = (decl.DateLimite - DateTime.Today).Days;
            if (daysLeft <= 15)
            {
                alerts.Add(new AlertInfo
                {
                    Message = $"Déclaration {decl.Periode} : échéance dans {daysLeft} jour(s) ({decl.DateLimite:dd/MM/yyyy})",
                    Severity = daysLeft <= 5 ? AlertSeverity.Error : AlertSeverity.Warning
                });
            }
        }

        var overdue = await db.Declarations
            .Where(d => d.EntityId == _tenant.EntityId)
            .Where(d => d.Statut == DeclarationStatut.AFaire && d.DateLimite < DateTime.Today)
            .ToListAsync();

        foreach (var decl in overdue)
        {
            alerts.Add(new AlertInfo
            {
                Message = $"Déclaration {decl.Periode} en retard ! Échéance dépassée le {decl.DateLimite:dd/MM/yyyy}",
                Severity = AlertSeverity.Error
            });
        }

        // Alerte double-comptage : charges fixes + import CSV actifs simultanément
        var hasFixedCharges = await db.FixedCharges
            .AnyAsync(f => f.EntityId == _tenant.EntityId);
        var hasImportedExpenses = await db.BankTransactions
            .AnyAsync(t => t.EntityId == _tenant.EntityId && t.ExpenseId != null);

        if (hasFixedCharges && hasImportedExpenses)
        {
            alerts.Add(new AlertInfo
            {
                Message = "Attention : des charges fixes et des dépenses importées par CSV sont actives simultanément. " +
                          "Vérifiez qu'il n'y a pas de double-comptage (ex: loyer déclaré en charge fixe ET importé depuis le relevé bancaire).",
                Severity = AlertSeverity.Warning
            });
        }

        return alerts;
    }
}

public class AlertInfo
{
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
}

public enum AlertSeverity
{
    Info,
    Warning,
    Error
}
