using Microsoft.EntityFrameworkCore;
using FrenchInvoice.Core.Data;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public class DashboardSummary
{
    public decimal CA { get; set; }
    public decimal Cotisations { get; set; }
    public decimal VersementLiberatoire { get; set; }
    public decimal FraisVariables { get; set; }
    public decimal ChargesFixes { get; set; }
    public decimal FraisPlateforme { get; set; }
    public decimal TotalCharges { get; set; }
    public decimal BeneficeNet { get; set; }
}

public class AccountingService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ITenantProvider _tenant;

    public AccountingService(IDbContextFactory<AppDbContext> factory, ITenantProvider tenant)
    {
        _factory = factory;
        _tenant = tenant;
    }

    public async Task<decimal> GetCAForPeriodAsync(DateTime start, DateTime end)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        return await db.Revenues
            .Where(r => r.EntityId == _tenant.EntityId)
            .Where(r => r.Date >= start && r.Date <= end)
            .SumAsync(r => r.Montant);
    }

    public async Task<decimal> GetExpensesForPeriodAsync(DateTime start, DateTime end)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        return await db.Expenses
            .Where(e => e.EntityId == _tenant.EntityId)
            .Where(e => e.Date >= start && e.Date <= end)
            .SumAsync(e => e.Montant);
    }

    public async Task<decimal> GetCAForYearAsync(int year)
    {
        var start = new DateTime(year, 1, 1);
        var end = new DateTime(year, 12, 31);
        return await GetCAForPeriodAsync(start, end);
    }

    public async Task<Entity> GetEntityAsync()
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        return await db.Entities.FirstAsync(e => e.Id == _tenant.EntityId);
    }

    public decimal GetTauxCotisation(ActivityCategory categorie) => categorie switch
    {
        ActivityCategory.BICVente => 12.3m,
        ActivityCategory.BICServices => 21.2m,
        ActivityCategory.BNC => 21.1m,
        _ => 21.1m
    };

    /// <summary>
    /// Returns the effective cotisation rate, halved if ACRE is active
    /// and we are within 1 year of DateDebutActivite.
    /// </summary>
    public decimal GetTauxCotisationEffectif(Entity settings)
    {
        var taux = GetTauxCotisation(settings.TypeActivite);
        if (settings.BeneficieACRE && settings.DateDebutActivite.HasValue)
        {
            var finACRE = settings.DateDebutActivite.Value.AddYears(1);
            if (DateTime.Today < finACRE)
            {
                taux /= 2m;
            }
        }
        return taux;
    }

    /// <summary>
    /// Calculates cotisations using the base rate (backward compatible).
    /// </summary>
    public decimal CalculerCotisations(decimal ca, ActivityCategory categorie)
    {
        return Math.Round(ca * GetTauxCotisation(categorie) / 100m, 2);
    }

    /// <summary>
    /// Calculates cotisations using Entity settings, applying ACRE if applicable.
    /// </summary>
    public decimal CalculerCotisations(decimal ca, Entity settings)
    {
        var taux = GetTauxCotisationEffectif(settings);
        return Math.Round(ca * taux / 100m, 2);
    }

    /// <summary>
    /// Returns ca * TauxLiberatoire / 100 if VersementLiberatoire is enabled, else 0.
    /// </summary>
    public decimal CalculerVersementLiberatoire(decimal ca, Entity settings)
    {
        if (!settings.VersementLiberatoire)
            return 0m;
        return Math.Round(ca * settings.TauxLiberatoire / 100m, 2);
    }

    /// <summary>
    /// Returns ca * FraisVariables / 100.
    /// </summary>
    public decimal CalculerFraisVariables(decimal ca, Entity settings)
    {
        return Math.Round(ca * settings.FraisVariables / 100m, 2);
    }

    /// <summary>
    /// Builds a dashboard summary for the given year (and optionally a single month).
    /// </summary>
    public async Task<DashboardSummary> GetDashboardSummaryAsync(int year, int? month = null)
    {
        await _tenant.InitializeAsync();
        var settings = await GetEntityAsync();

        DateTime start, end;
        int nbMois;

        if (month.HasValue)
        {
            start = new DateTime(year, month.Value, 1);
            end = start.AddMonths(1).AddDays(-1);
            nbMois = 1;
        }
        else
        {
            start = new DateTime(year, 1, 1);
            end = new DateTime(year, 12, 31);
            nbMois = 12;
        }

        var ca = await GetCAForPeriodAsync(start, end);
        var cotisations = CalculerCotisations(ca, settings);
        var versementLiberatoire = CalculerVersementLiberatoire(ca, settings);
        var fraisVariables = CalculerFraisVariables(ca, settings);

        using var db = _factory.CreateDbContext();
        var chargesFixesMensuelles = await db.FixedCharges
            .Where(fc => fc.EntityId == _tenant.EntityId)
            .Where(fc => fc.Active)
            .SumAsync(fc => fc.Montant);
        var chargesFixes = chargesFixesMensuelles * nbMois;

        // Frais de plateforme (commissions Stancer/Stripe) sur la période
        var fraisPlateforme = await db.PayoutRecords
            .Where(p => p.EntityId == _tenant.EntityId)
            .Where(p => p.Date >= start && p.Date <= end)
            .SumAsync(p => (decimal?)p.Frais) ?? 0;

        var totalCharges = cotisations + versementLiberatoire + fraisVariables + chargesFixes + fraisPlateforme;
        var beneficeNet = ca - totalCharges;

        return new DashboardSummary
        {
            CA = ca,
            Cotisations = cotisations,
            VersementLiberatoire = versementLiberatoire,
            FraisVariables = fraisVariables,
            ChargesFixes = chargesFixes,
            FraisPlateforme = fraisPlateforme,
            TotalCharges = totalCharges,
            BeneficeNet = beneficeNet
        };
    }
}
