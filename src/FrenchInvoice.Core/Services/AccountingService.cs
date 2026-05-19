using Microsoft.EntityFrameworkCore;
using FrenchInvoice.Core.Data;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public class DashboardSummary
{
    public decimal CA { get; set; }
    public decimal Depenses { get; set; }
    public decimal Cotisations { get; set; }
    public decimal CFP { get; set; }
    public decimal VersementLiberatoire { get; set; }
    public decimal FraisVariables { get; set; }
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

    public decimal GetTauxCotisation(Entity settings) => settings.TauxCotisation;

    public decimal GetTauxCFP(Entity settings) => settings.TauxCFP;

    public decimal CalculerCFP(decimal ca, Entity settings)
    {
        var assiette = TaxRuleEngine.GetAssietteCotisations(settings.StatutJuridique, ca, 0);
        return TaxRuleEngine.CalculerCFP(settings.StatutJuridique, assiette, settings.TauxCFP);
    }

    public decimal CalculerCFP(decimal ca, decimal depenses, Entity settings)
    {
        var assiette = TaxRuleEngine.GetAssietteCotisations(settings.StatutJuridique, ca, depenses);
        return TaxRuleEngine.CalculerCFP(settings.StatutJuridique, assiette, settings.TauxCFP);
    }

    public decimal GetTauxCotisationEffectif(Entity settings)
    {
        return TaxRuleEngine.GetTauxEffectif(settings.StatutJuridique, settings.TauxCotisation,
            settings.BeneficieACRE, settings.DateDebutActivite);
    }

    public decimal CalculerCotisations(decimal ca, Entity settings)
    {
        var assiette = TaxRuleEngine.GetAssietteCotisations(settings.StatutJuridique, ca, 0);
        return TaxRuleEngine.CalculerCotisations(settings.StatutJuridique, assiette, settings.TauxCotisation,
            settings.BeneficieACRE, settings.DateDebutActivite);
    }

    public decimal CalculerCotisations(decimal ca, decimal depenses, Entity settings)
    {
        var assiette = TaxRuleEngine.GetAssietteCotisations(settings.StatutJuridique, ca, depenses);
        return TaxRuleEngine.CalculerCotisations(settings.StatutJuridique, assiette, settings.TauxCotisation,
            settings.BeneficieACRE, settings.DateDebutActivite);
    }

    public decimal CalculerVersementLiberatoire(decimal ca, Entity settings)
    {
        if (!settings.VersementLiberatoire || !TaxRuleEngine.VersementLiberatoireDisponible(settings.StatutJuridique))
            return 0m;
        return Math.Round(ca * settings.TauxLiberatoire / 100m, 2);
    }

    public decimal CalculerFraisVariables(decimal ca, Entity settings)
    {
        return Math.Round(ca * settings.FraisVariables / 100m, 2);
    }

    /// <summary>
    /// Builds a dashboard summary for the given year (and optionally a single month).
    /// </summary>
    public async Task<List<(int Mois, decimal CA)>> GetCAMensuelHistoriqueAsync(int entityId, int moisCount)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        var result = new List<(int, decimal)>();
        var now = DateTime.Today;

        for (int i = moisCount - 1; i >= 0; i--)
        {
            var moisDate = now.AddMonths(-i);
            var start = new DateTime(moisDate.Year, moisDate.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            var ca = await db.Revenues
                .Where(r => r.EntityId == entityId && r.Date >= start && r.Date <= end)
                .SumAsync(r => (decimal?)r.Montant) ?? 0;
            result.Add((moisCount - i, ca));
        }
        return result;
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync(int year, int? month = null)
    {
        await _tenant.InitializeAsync();
        var settings = await GetEntityAsync();

        DateTime start, end;

        if (month.HasValue)
        {
            start = new DateTime(year, month.Value, 1);
            end = start.AddMonths(1).AddDays(-1);
        }
        else
        {
            start = new DateTime(year, 1, 1);
            end = new DateTime(year, 12, 31);
        }

        var ca = await GetCAForPeriodAsync(start, end);
        var depenses = await GetExpensesForPeriodAsync(start, end);
        var cotisations = CalculerCotisations(ca, depenses, settings);
        var cfp = CalculerCFP(ca, depenses, settings);
        var versementLiberatoire = CalculerVersementLiberatoire(ca, settings);
        var fraisVariables = CalculerFraisVariables(ca, settings);

        using var db = _factory.CreateDbContext();

        // Frais de plateforme (commissions) sur la periode
        var fraisPlateforme = await db.PayoutRecords
            .Where(p => p.EntityId == _tenant.EntityId)
            .Where(p => p.Date >= start && p.Date <= end)
            .SumAsync(p => (decimal?)p.Frais) ?? 0;

        var totalCharges = cotisations + cfp + versementLiberatoire + fraisVariables + fraisPlateforme;
        var beneficeNet = settings.StatutJuridique == LegalStatus.MicroEntreprise
            ? ca - totalCharges
            : ca - depenses - totalCharges;

        return new DashboardSummary
        {
            CA = ca,
            Depenses = depenses,
            Cotisations = cotisations,
            CFP = cfp,
            VersementLiberatoire = versementLiberatoire,
            FraisVariables = fraisVariables,
            FraisPlateforme = fraisPlateforme,
            TotalCharges = totalCharges,
            BeneficeNet = beneficeNet
        };
    }
}
