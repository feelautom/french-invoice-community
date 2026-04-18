using Microsoft.EntityFrameworkCore;
using FrenchInvoice.Core.Data;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public class DeclarationService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly AccountingService _accounting;
    private readonly ITenantProvider _tenant;

    public DeclarationService(IDbContextFactory<AppDbContext> factory, AccountingService accounting,
        ITenantProvider tenant)
    {
        _factory = factory;
        _accounting = accounting;
        _tenant = tenant;
    }

    public async Task<List<Declaration>> GetAllAsync()
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        return await db.Declarations
            .Where(d => d.EntityId == _tenant.EntityId)
            .OrderByDescending(d => d.DateLimite).ToListAsync();
    }

    public async Task<List<Declaration>> GetByYearAsync(int year)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        var yearPrefix = $"{year}-";
        return await db.Declarations
            .Where(d => d.EntityId == _tenant.EntityId)
            .Where(d => d.Periode.StartsWith(yearPrefix))
            .OrderBy(d => d.PeriodeDebut)
            .ToListAsync();
    }

    public async Task<Declaration?> GetByIdAsync(int id)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        return await db.Declarations
            .Where(d => d.EntityId == _tenant.EntityId)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task SaveAsync(Declaration declaration)
    {
        await _tenant.InitializeAsync();
        using var db = _factory.CreateDbContext();
        if (declaration.Id == 0)
        {
            declaration.EntityId = _tenant.EntityId;
            db.Declarations.Add(declaration);
        }
        else
            db.Declarations.Update(declaration);
        await db.SaveChangesAsync();
    }

    public async Task GenerateDeclarationsAsync(int year)
    {
        await _tenant.InitializeAsync();
        var settings = await _accounting.GetEntityAsync();
        using var db = _factory.CreateDbContext();

        var periods = settings.PeriodiciteDeclaration == DeclarationPeriodicity.Mensuelle
            ? GetMonthlyPeriods(year)
            : GetQuarterlyPeriods(year);

        var yearPrefix = $"{year}-";
        var existingAFaire = await db.Declarations
            .Where(d => d.EntityId == _tenant.EntityId)
            .Where(d => d.Periode.StartsWith(yearPrefix) && d.Statut == DeclarationStatut.AFaire)
            .ToListAsync();
        db.Declarations.RemoveRange(existingAFaire);
        await db.SaveChangesAsync();

        DateTime? deadlineCarence = null;
        if (settings.DateDebutActivite.HasValue)
        {
            var debut = settings.DateDebutActivite.Value;
            periods = periods.Where(p => p.End >= debut).ToList();

            var finCarence = debut.AddDays(90);
            var moisFinCarence = new DateTime(finCarence.Year, finCarence.Month, 1);
            deadlineCarence = moisFinCarence.AddMonths(2).AddDays(-1);
        }

        foreach (var (label, start, end, deadline) in periods)
        {
            var existing = await db.Declarations
                .Where(d => d.EntityId == _tenant.EntityId)
                .FirstOrDefaultAsync(d => d.Periode == label);
            if (existing != null)
                continue;

            var ca = await _accounting.GetCAForPeriodAsync(start, end);
            var taux = _accounting.GetTauxCotisationEffectif(settings);
            var cotisations = _accounting.CalculerCotisations(ca, settings);

            var effectiveDeadline = deadline;
            if (deadlineCarence.HasValue && settings.DateDebutActivite.HasValue
                && start < settings.DateDebutActivite.Value.AddDays(90))
            {
                effectiveDeadline = deadlineCarence.Value > deadline ? deadlineCarence.Value : deadline;
            }

            db.Declarations.Add(new Declaration
            {
                EntityId = _tenant.EntityId,
                Periode = label,
                PeriodeDebut = start,
                PeriodeFin = end,
                DateLimite = effectiveDeadline,
                MontantCA = ca,
                MontantCotisations = cotisations,
                TauxApplique = taux,
                Statut = DeclarationStatut.AFaire
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task RecalculerAsync(int year)
    {
        await _tenant.InitializeAsync();
        var settings = await _accounting.GetEntityAsync();
        using var db = _factory.CreateDbContext();

        var yearPrefix = $"{year}-";
        var declarations = await db.Declarations
            .Where(d => d.EntityId == _tenant.EntityId)
            .Where(d => d.Periode.StartsWith(yearPrefix) && d.Statut == DeclarationStatut.AFaire)
            .ToListAsync();

        foreach (var decl in declarations)
        {
            var ca = await _accounting.GetCAForPeriodAsync(decl.PeriodeDebut, decl.PeriodeFin);
            decl.MontantCA = ca;
            decl.MontantCotisations = _accounting.CalculerCotisations(ca, settings);
            decl.TauxApplique = _accounting.GetTauxCotisationEffectif(settings);
            db.Declarations.Update(decl);
        }

        await db.SaveChangesAsync();
    }

    private static List<(string Label, DateTime Start, DateTime End, DateTime Deadline)> GetMonthlyPeriods(int year)
    {
        var periods = new List<(string, DateTime, DateTime, DateTime)>();
        for (int m = 1; m <= 12; m++)
        {
            var start = new DateTime(year, m, 1);
            var end = start.AddMonths(1).AddDays(-1);
            var deadlineMonth = start.AddMonths(1);
            var deadline = new DateTime(deadlineMonth.Year, deadlineMonth.Month,
                DateTime.DaysInMonth(deadlineMonth.Year, deadlineMonth.Month));
            periods.Add(($"{year}-{m:D2}", start, end, deadline));
        }
        return periods;
    }

    private static List<(string Label, DateTime Start, DateTime End, DateTime Deadline)> GetQuarterlyPeriods(int year)
    {
        var periods = new List<(string, DateTime, DateTime, DateTime)>();
        for (int q = 1; q <= 4; q++)
        {
            var startMonth = (q - 1) * 3 + 1;
            var start = new DateTime(year, startMonth, 1);
            var end = start.AddMonths(3).AddDays(-1);
            var deadlineMonth = end.AddDays(1);
            var deadline = new DateTime(deadlineMonth.Year, deadlineMonth.Month,
                DateTime.DaysInMonth(deadlineMonth.Year, deadlineMonth.Month));
            periods.Add(($"{year}-T{q}", start, end, deadline));
        }
        return periods;
    }
}
