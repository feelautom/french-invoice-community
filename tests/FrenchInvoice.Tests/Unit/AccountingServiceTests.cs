using FluentAssertions;
using FrenchInvoice.Core.Models;
using FrenchInvoice.Core.Services;
using FrenchInvoice.Tests.Fixtures;

namespace FrenchInvoice.Tests.Unit;

public class AccountingServiceTests : IDisposable
{
    private readonly DatabaseFixture _db = new();

    // ── Taux de cotisations ──

    [Fact]
    public void GetTauxCotisation_ReturnsEntityRate()
    {
        var entity = new Entity { TauxCotisation = 25.6m };
        var svc = CreateService(1);
        svc.GetTauxCotisation(entity).Should().Be(25.6m);
    }

    // ── ACRE ──

    [Fact]
    public void GetTauxCotisationEffectif_SansACRE_ReturnsTauxNormal()
    {
        var entity = new Entity { TauxCotisation = 25.6m, BeneficieACRE = false };
        var svc = CreateService(1);
        svc.GetTauxCotisationEffectif(entity).Should().Be(25.6m);
    }

    [Fact]
    public void GetTauxCotisationEffectif_AvecACRE_DansPremièreAnnée_RetourneTauxDiviséPar2()
    {
        var entity = new Entity
        {
            TauxCotisation = 25.6m,
            BeneficieACRE = true,
            DateDebutActivite = DateTime.Today.AddMonths(-6)
        };
        var svc = CreateService(1);
        svc.GetTauxCotisationEffectif(entity).Should().Be(25.6m / 2m);
    }

    [Fact]
    public void GetTauxCotisationEffectif_AvecACRE_AprèsPremièreAnnée_RetourneTauxNormal()
    {
        var entity = new Entity
        {
            TauxCotisation = 21.2m,
            BeneficieACRE = true,
            DateDebutActivite = DateTime.Today.AddYears(-2)
        };
        var svc = CreateService(1);
        svc.GetTauxCotisationEffectif(entity).Should().Be(21.2m);
    }

    [Fact]
    public void GetTauxCotisationEffectif_AvecACRE_SansDateDebut_RetourneTauxNormal()
    {
        var entity = new Entity
        {
            TauxCotisation = 25.6m,
            BeneficieACRE = true,
            DateDebutActivite = null
        };
        var svc = CreateService(1);
        svc.GetTauxCotisationEffectif(entity).Should().Be(25.6m);
    }

    // ── Calcul cotisations ──

    [Fact]
    public void CalculerCotisations_UtiliseTauxEntity()
    {
        var entity = new Entity { TauxCotisation = 25.6m, BeneficieACRE = false };
        var svc = CreateService(1);
        svc.CalculerCotisations(10000m, entity).Should().Be(2560m);
    }

    [Fact]
    public void CalculerCotisations_AvecSettingsACRE_DiviseParDeux()
    {
        var entity = new Entity
        {
            TauxCotisation = 25.6m,
            BeneficieACRE = true,
            DateDebutActivite = DateTime.Today.AddMonths(-3)
        };
        var svc = CreateService(1);
        svc.CalculerCotisations(10000m, entity).Should().Be(1280m);
    }

    // ── Versement libératoire ──

    [Fact]
    public void CalculerVersementLiberatoire_QuandActif_CalculeCorrectement()
    {
        var entity = new Entity { VersementLiberatoire = true, TauxLiberatoire = 2.2m };
        var svc = CreateService(1);
        svc.CalculerVersementLiberatoire(10000m, entity).Should().Be(220m);
    }

    [Fact]
    public void CalculerVersementLiberatoire_QuandInactif_RetourneZero()
    {
        var entity = new Entity { VersementLiberatoire = false, TauxLiberatoire = 2.2m };
        var svc = CreateService(1);
        svc.CalculerVersementLiberatoire(10000m, entity).Should().Be(0m);
    }

    // ── Frais variables ──

    [Fact]
    public void CalculerFraisVariables_CalculeCorrectement()
    {
        var entity = new Entity { FraisVariables = 5m };
        var svc = CreateService(1);
        svc.CalculerFraisVariables(10000m, entity).Should().Be(500m);
    }

    // ── CA par période (avec DB) ──

    [Fact]
    public async Task GetCAForPeriodAsync_FiltreDateEtEntité()
    {
        using var db = _db.CreateDbContext();
        var entity = _db.SeedEntity(db);
        var tenant = new TestTenantProvider(entity.Id);
        var svc = new AccountingService(_db.CreateFactory(), tenant);

        // Revenus de l'entité 1
        db.Revenues.Add(new Revenue { EntityId = entity.Id, Date = new DateTime(2026, 3, 15), Montant = 1000m, Description = "R1" });
        db.Revenues.Add(new Revenue { EntityId = entity.Id, Date = new DateTime(2026, 3, 20), Montant = 500m, Description = "R2" });
        // Revenu hors période
        db.Revenues.Add(new Revenue { EntityId = entity.Id, Date = new DateTime(2026, 5, 1), Montant = 2000m, Description = "R3" });
        db.SaveChanges();

        var ca = await svc.GetCAForPeriodAsync(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));
        ca.Should().Be(1500m);
    }

    [Fact]
    public async Task GetCAForPeriodAsync_NeVoitPasAutreEntité()
    {
        using var db = _db.CreateDbContext();
        var entity1 = _db.SeedEntity(db, "Entity 1");
        var entity2 = _db.SeedEntity(db, "Entity 2");

        db.Revenues.Add(new Revenue { EntityId = entity1.Id, Date = new DateTime(2026, 3, 15), Montant = 1000m, Description = "R1" });
        db.Revenues.Add(new Revenue { EntityId = entity2.Id, Date = new DateTime(2026, 3, 15), Montant = 9999m, Description = "R-other" });
        db.SaveChanges();

        var tenant1 = new TestTenantProvider(entity1.Id);
        var svc = new AccountingService(_db.CreateFactory(), tenant1);

        var ca = await svc.GetCAForPeriodAsync(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));
        ca.Should().Be(1000m);
    }

    // ── Dashboard ──

    [Fact]
    public async Task GetDashboardSummaryAsync_AgregeCorrectement()
    {
        using var db = _db.CreateDbContext();
        var entity = _db.SeedEntity(db);
        var tenant = new TestTenantProvider(entity.Id);
        var svc = new AccountingService(_db.CreateFactory(), tenant);

        db.Revenues.Add(new Revenue { EntityId = entity.Id, Date = new DateTime(2026, 6, 15), Montant = 5000m, Description = "R1" });
        db.SaveChanges();

        var summary = await svc.GetDashboardSummaryAsync(2026, 6);

        summary.CA.Should().Be(5000m);
        summary.Cotisations.Should().BeGreaterThan(0);
        summary.BeneficeNet.Should().Be(summary.CA - summary.TotalCharges);
    }

    private AccountingService CreateService(int entityId)
    {
        return new AccountingService(_db.CreateFactory(), new TestTenantProvider(entityId));
    }

    public void Dispose() => _db.Dispose();
}
