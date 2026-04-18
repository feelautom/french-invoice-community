using FluentAssertions;
using FrenchInvoice.Core.Models;
using FrenchInvoice.Core.Services;
using FrenchInvoice.Tests.Fixtures;

namespace FrenchInvoice.Tests.Unit;

public class AccountingServiceTests : IDisposable
{
    private readonly DatabaseFixture _db = new();

    // ── Taux de cotisations ──

    [Theory]
    [InlineData(ActivityCategory.BICVente, 12.3)]
    [InlineData(ActivityCategory.BICServices, 21.2)]
    [InlineData(ActivityCategory.BNC, 21.1)]
    public void GetTauxCotisation_ReturnsCorrectRate(ActivityCategory cat, decimal expected)
    {
        var svc = CreateService(1);
        svc.GetTauxCotisation(cat).Should().Be(expected);
    }

    // ── ACRE ──

    [Fact]
    public void GetTauxCotisationEffectif_SansACRE_ReturnsTauxNormal()
    {
        var entity = new Entity { TypeActivite = ActivityCategory.BNC, BeneficieACRE = false };
        var svc = CreateService(1);
        svc.GetTauxCotisationEffectif(entity).Should().Be(21.1m);
    }

    [Fact]
    public void GetTauxCotisationEffectif_AvecACRE_DansPremièreAnnée_RetourneTauxDiviséPar2()
    {
        var entity = new Entity
        {
            TypeActivite = ActivityCategory.BNC,
            BeneficieACRE = true,
            DateDebutActivite = DateTime.Today.AddMonths(-6) // il y a 6 mois
        };
        var svc = CreateService(1);
        svc.GetTauxCotisationEffectif(entity).Should().Be(21.1m / 2m);
    }

    [Fact]
    public void GetTauxCotisationEffectif_AvecACRE_AprèsPremièreAnnée_RetourneTauxNormal()
    {
        var entity = new Entity
        {
            TypeActivite = ActivityCategory.BICServices,
            BeneficieACRE = true,
            DateDebutActivite = DateTime.Today.AddYears(-2) // il y a 2 ans
        };
        var svc = CreateService(1);
        svc.GetTauxCotisationEffectif(entity).Should().Be(21.2m);
    }

    [Fact]
    public void GetTauxCotisationEffectif_AvecACRE_SansDateDebut_RetourneTauxNormal()
    {
        var entity = new Entity
        {
            TypeActivite = ActivityCategory.BNC,
            BeneficieACRE = true,
            DateDebutActivite = null
        };
        var svc = CreateService(1);
        svc.GetTauxCotisationEffectif(entity).Should().Be(21.1m);
    }

    // ── Calcul cotisations ──

    [Theory]
    [InlineData(10000, ActivityCategory.BICVente, 1230)]
    [InlineData(10000, ActivityCategory.BICServices, 2120)]
    [InlineData(10000, ActivityCategory.BNC, 2110)]
    public void CalculerCotisations_ParCategorie(decimal ca, ActivityCategory cat, decimal expected)
    {
        var svc = CreateService(1);
        svc.CalculerCotisations(ca, cat).Should().Be(expected);
    }

    [Fact]
    public void CalculerCotisations_AvecSettingsACRE_DiviseParDeux()
    {
        var entity = new Entity
        {
            TypeActivite = ActivityCategory.BNC,
            BeneficieACRE = true,
            DateDebutActivite = DateTime.Today.AddMonths(-3)
        };
        var svc = CreateService(1);
        // 10000 * (21.1/2) / 100 = 1055
        svc.CalculerCotisations(10000m, entity).Should().Be(1055m);
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
        db.FixedCharges.Add(new FixedCharge { EntityId = entity.Id, Nom = "Loyer", Montant = 100m, Active = true });
        db.SaveChanges();

        var summary = await svc.GetDashboardSummaryAsync(2026, 6);

        summary.CA.Should().Be(5000m);
        summary.Cotisations.Should().BeGreaterThan(0);
        summary.ChargesFixes.Should().Be(100m); // 1 mois
        summary.BeneficeNet.Should().Be(summary.CA - summary.TotalCharges);
    }

    private AccountingService CreateService(int entityId)
    {
        return new AccountingService(_db.CreateFactory(), new TestTenantProvider(entityId));
    }

    public void Dispose() => _db.Dispose();
}
