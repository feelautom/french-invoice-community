using FluentAssertions;
using FrenchInvoice.Core.Models;
using FrenchInvoice.Core.Services;
using FrenchInvoice.Tests.Fixtures;

namespace FrenchInvoice.Tests.Unit;

public class DeclarationServiceTests : IDisposable
{
    private readonly DatabaseFixture _db = new();

    // ── Génération mensuelle ──

    [Fact]
    public async Task GenerateDeclarationsAsync_Mensuel_Genere12Periodes()
    {
        using var db = _db.CreateDbContext();
        var entity = _db.SeedEntity(db);
        entity.PeriodiciteDeclaration = DeclarationPeriodicity.Mensuelle;
        entity.DateDebutActivite = new DateTime(2025, 1, 1); // bien avant 2026
        db.SaveChanges();

        var svc = CreateService(entity.Id);
        await svc.GenerateDeclarationsAsync(2026);

        var decls = await svc.GetByYearAsync(2026);
        decls.Should().HaveCount(12);
        decls.First().Periode.Should().Be("2026-01");
        decls.Last().Periode.Should().Be("2026-12");
    }

    [Fact]
    public async Task GenerateDeclarationsAsync_Mensuel_DeadlineFinMoisSuivant()
    {
        using var db = _db.CreateDbContext();
        var entity = _db.SeedEntity(db);
        entity.DateDebutActivite = new DateTime(2025, 1, 1);
        db.SaveChanges();

        var svc = CreateService(entity.Id);
        await svc.GenerateDeclarationsAsync(2026);

        var decls = await svc.GetByYearAsync(2026);
        // Janvier 2026 → deadline 28 février 2026
        var jan = decls.First(d => d.Periode == "2026-01");
        jan.DateLimite.Should().Be(new DateTime(2026, 2, 28));
    }

    // ── Génération trimestrielle ──

    [Fact]
    public async Task GenerateDeclarationsAsync_Trimestriel_Genere4Periodes()
    {
        using var db = _db.CreateDbContext();
        var entity = _db.SeedEntity(db);
        entity.PeriodiciteDeclaration = DeclarationPeriodicity.Trimestrielle;
        entity.DateDebutActivite = new DateTime(2025, 1, 1);
        db.SaveChanges();

        var svc = CreateService(entity.Id);
        await svc.GenerateDeclarationsAsync(2026);

        var decls = await svc.GetByYearAsync(2026);
        decls.Should().HaveCount(4);
        decls[0].Periode.Should().Be("2026-T1");
        decls[1].Periode.Should().Be("2026-T2");
        decls[2].Periode.Should().Be("2026-T3");
        decls[3].Periode.Should().Be("2026-T4");
    }

    [Fact]
    public async Task GenerateDeclarationsAsync_Trimestriel_DeadlineCorrecte()
    {
        using var db = _db.CreateDbContext();
        var entity = _db.SeedEntity(db);
        entity.PeriodiciteDeclaration = DeclarationPeriodicity.Trimestrielle;
        entity.DateDebutActivite = new DateTime(2025, 1, 1);
        db.SaveChanges();

        var svc = CreateService(entity.Id);
        await svc.GenerateDeclarationsAsync(2026);

        var decls = await svc.GetByYearAsync(2026);
        // T1 (Jan-Mar) → deadline 30 avril
        decls[0].DateLimite.Should().Be(new DateTime(2026, 4, 30));
        // T4 (Oct-Dec) → deadline 31 janvier 2027
        decls[3].DateLimite.Should().Be(new DateTime(2027, 1, 31));
    }

    // ── Filtre par date de début d'activité ──

    [Fact]
    public async Task GenerateDeclarationsAsync_FiltrePériodesAvantDébutActivité()
    {
        using var db = _db.CreateDbContext();
        var entity = _db.SeedEntity(db);
        entity.DateDebutActivite = new DateTime(2026, 6, 1); // début en juin
        db.SaveChanges();

        var svc = CreateService(entity.Id);
        await svc.GenerateDeclarationsAsync(2026);

        var decls = await svc.GetByYearAsync(2026);
        // Ne devrait pas avoir de périodes avant juin (carence 90j fusionne juin-août)
        decls.Should().AllSatisfy(d => d.PeriodeFin.Should().BeOnOrAfter(new DateTime(2026, 6, 1)));
    }

    // ── Carence 90 jours : mois par mois avec deadline repoussée ──

    [Fact]
    public async Task GenerateDeclarationsAsync_Carence90j_MoisParMoisAvecDeadlineRepoussée()
    {
        using var db = _db.CreateDbContext();
        var entity = _db.SeedEntity(db);
        entity.DateDebutActivite = new DateTime(2026, 2, 1); // début en février
        db.SaveChanges();

        var svc = CreateService(entity.Id);
        await svc.GenerateDeclarationsAsync(2026);

        var decls = await svc.GetByYearAsync(2026);
        var ordered = decls.OrderBy(d => d.PeriodeDebut).ToList();

        // Chaque mois est une déclaration séparée (pas de fusion)
        ordered.First().Periode.Should().Be("2026-02");
        ordered.Should().HaveCount(11); // fév à déc
        ordered.Should().OnlyContain(d => !d.Periode.Contains("à"));

        // Carence 90j depuis 1er fév = 2 mai → deadline repoussée au 30/06/2026
        var deadlineCarence = new DateTime(2026, 6, 30);
        ordered.First(d => d.Periode == "2026-02").DateLimite.Should().Be(deadlineCarence);
        ordered.First(d => d.Periode == "2026-03").DateLimite.Should().Be(deadlineCarence);
        ordered.First(d => d.Periode == "2026-04").DateLimite.Should().Be(deadlineCarence);
        ordered.First(d => d.Periode == "2026-05").DateLimite.Should().Be(deadlineCarence);

        // Juin et après : deadline normale (fin du mois suivant)
        ordered.First(d => d.Periode == "2026-06").DateLimite.Should().Be(new DateTime(2026, 7, 31));
    }

    // ── Ne recréé pas les déclarations déjà traitées ──

    [Fact]
    public async Task GenerateDeclarationsAsync_NeTouchePasDeclarationsDéjàDéclarées()
    {
        using var db = _db.CreateDbContext();
        var entity = _db.SeedEntity(db);
        entity.DateDebutActivite = new DateTime(2025, 1, 1);
        db.SaveChanges();

        var svc = CreateService(entity.Id);
        await svc.GenerateDeclarationsAsync(2026);

        // Marquer janvier comme déclarée
        var decls = await svc.GetByYearAsync(2026);
        var jan = decls.First(d => d.Periode == "2026-01");
        jan.Statut = DeclarationStatut.Declaree;
        jan.DateDeclaration = DateTime.Today;
        await svc.SaveAsync(jan);

        // Regénérer
        await svc.GenerateDeclarationsAsync(2026);

        decls = await svc.GetByYearAsync(2026);
        var janAfter = decls.First(d => d.Periode == "2026-01");
        janAfter.Statut.Should().Be(DeclarationStatut.Declaree);
    }

    // ── Recalculer ──

    [Fact]
    public async Task RecalculerAsync_MajMontantsAFaireUniquement()
    {
        using var db = _db.CreateDbContext();
        var entity = _db.SeedEntity(db);
        entity.DateDebutActivite = new DateTime(2025, 1, 1);
        db.SaveChanges();

        var svc = CreateService(entity.Id);
        await svc.GenerateDeclarationsAsync(2026);

        // Ajouter un revenu en mars
        db.Revenues.Add(new Revenue
        {
            EntityId = entity.Id,
            Date = new DateTime(2026, 3, 15),
            Montant = 5000m,
            Description = "Vente"
        });
        db.SaveChanges();

        await svc.RecalculerAsync(2026);

        var decls = await svc.GetByYearAsync(2026);
        var mars = decls.First(d => d.Periode == "2026-03");
        mars.MontantCA.Should().Be(5000m);
        mars.MontantCotisations.Should().BeGreaterThan(0);
    }

    // ── Isolation multi-tenant ──

    [Fact]
    public async Task GetByYearAsync_NeVoitPasAutreEntité()
    {
        using var db = _db.CreateDbContext();
        var entity1 = _db.SeedEntity(db, "Entity 1");
        var entity2 = _db.SeedEntity(db, "Entity 2");
        entity1.DateDebutActivite = new DateTime(2025, 1, 1);
        entity2.DateDebutActivite = new DateTime(2025, 1, 1);
        db.SaveChanges();

        var svc1 = CreateService(entity1.Id);
        var svc2 = CreateService(entity2.Id);

        await svc1.GenerateDeclarationsAsync(2026);
        await svc2.GenerateDeclarationsAsync(2026);

        var decls1 = await svc1.GetByYearAsync(2026);
        var decls2 = await svc2.GetByYearAsync(2026);

        decls1.Should().AllSatisfy(d => d.EntityId.Should().Be(entity1.Id));
        decls2.Should().AllSatisfy(d => d.EntityId.Should().Be(entity2.Id));
    }

    private DeclarationService CreateService(int entityId)
    {
        var tenant = new TestTenantProvider(entityId);
        var accounting = new AccountingService(_db.CreateFactory(), tenant);
        return new DeclarationService(_db.CreateFactory(), accounting, tenant);
    }

    public void Dispose() => _db.Dispose();
}
