using Microsoft.EntityFrameworkCore;
using FrenchInvoice.Core.Data;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public class AnnualReportService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly AccountingService _accounting;
    private readonly IncomeTaxService _taxService;
    private readonly ITenantProvider _tenant;

    public AnnualReportService(
        IDbContextFactory<AppDbContext> factory,
        AccountingService accounting,
        IncomeTaxService taxService,
        ITenantProvider tenant)
    {
        _factory = factory;
        _accounting = accounting;
        _taxService = taxService;
        _tenant = tenant;
    }

    public async Task<AnnualReport> GenerateAsync(int entityId, int annee)
    {
        using var db = _factory.CreateDbContext();

        var entity = await db.Entities.FirstAsync(e => e.Id == entityId);
        var start = new DateTime(annee, 1, 1);
        var end = new DateTime(annee, 12, 31);

        // Chiffre d'affaires
        var ca = await db.Revenues
            .Where(r => r.EntityId == entityId && r.Date >= start && r.Date <= end)
            .SumAsync(r => (decimal?)r.Montant) ?? 0;

        // Depenses (hors apports personnels)
        var depenses = await db.Expenses
            .Where(e => e.EntityId == entityId && e.Date >= start && e.Date <= end)
            .SumAsync(e => (decimal?)e.Montant) ?? 0;

        // Frais plateforme
        var fraisPlateforme = await db.PayoutRecords
            .Where(p => p.EntityId == entityId && p.Date >= start && p.Date <= end)
            .SumAsync(p => (decimal?)p.Frais) ?? 0;

        // Cotisations et CFP
        var cotisations = _accounting.CalculerCotisations(ca, depenses, entity);
        var cfp = _accounting.CalculerCFP(ca, depenses, entity);
        var vl = _accounting.CalculerVersementLiberatoire(ca, entity);
        var fraisVariables = _accounting.CalculerFraisVariables(ca, entity);

        var totalProduits = ca;
        var totalCharges = depenses + cotisations + cfp + vl + fraisVariables + fraisPlateforme;
        var resultatExploitation = totalProduits - totalCharges;

        // IS (SASU)
        decimal impotSurBenefice = 0;
        if (TaxRuleEngine.EstSoumisIS(entity.StatutJuridique) && resultatExploitation > 0)
        {
            var isResult = _taxService.CalculerIS(resultatExploitation);
            impotSurBenefice = isResult.TotalIS;
        }

        var resultatNet = resultatExploitation - impotSurBenefice;

        // Bilan simplifie - Actif
        var lastBankTx = await db.BankTransactions
            .Where(b => b.EntityId == entityId && b.Date <= end && b.Solde != null)
            .OrderByDescending(b => b.Date)
            .ThenByDescending(b => b.Id)
            .FirstOrDefaultAsync();
        var tresorerie = lastBankTx?.Solde ?? 0;

        var creancesClients = await db.Invoices
            .Where(i => i.EntityId == entityId && i.DateEmission <= end
                && i.Statut != InvoiceStatus.Payee && i.Statut != InvoiceStatus.Annulee)
            .SumAsync(i => (decimal?)i.MontantTTC) ?? 0;

        // Bilan simplifie - Passif
        var apports = await db.BankTransactions
            .Where(b => b.EntityId == entityId && b.Date >= start && b.Date <= end
                && b.Type == BankTransactionType.PersonalContribution)
            .SumAsync(b => (decimal?)b.Montant) ?? 0;

        var dettesUrssaf = await db.Declarations
            .Where(d => d.EntityId == entityId && d.PeriodeFin <= end
                && d.Statut == DeclarationStatut.AFaire)
            .SumAsync(d => (decimal?)d.MontantCotisations) ?? 0;

        // Verifier si un rapport existe deja pour cette annee
        var existing = await db.AnnualReports
            .FirstOrDefaultAsync(r => r.EntityId == entityId && r.Annee == annee);

        if (existing != null)
        {
            // Mettre a jour
            MapReport(existing, entity, annee, start, end, ca, depenses, fraisPlateforme,
                cotisations, cfp, vl, fraisVariables, totalProduits, totalCharges,
                resultatExploitation, impotSurBenefice, resultatNet,
                tresorerie, creancesClients, apports, dettesUrssaf);
            existing.GeneratedAt = DateTime.UtcNow;
            db.AnnualReports.Update(existing);
            await db.SaveChangesAsync();
            return existing;
        }

        var report = new AnnualReport();
        MapReport(report, entity, annee, start, end, ca, depenses, fraisPlateforme,
            cotisations, cfp, vl, fraisVariables, totalProduits, totalCharges,
            resultatExploitation, impotSurBenefice, resultatNet,
            tresorerie, creancesClients, apports, dettesUrssaf);
        report.EntityId = entityId;

        db.AnnualReports.Add(report);
        await db.SaveChangesAsync();
        return report;
    }

    public async Task<List<AnnualReport>> GetReportsAsync(int entityId)
    {
        using var db = _factory.CreateDbContext();
        return await db.AnnualReports
            .Where(r => r.EntityId == entityId)
            .OrderByDescending(r => r.Annee)
            .ToListAsync();
    }

    private static void MapReport(AnnualReport r, Entity entity, int annee,
        DateTime start, DateTime end, decimal ca, decimal depenses, decimal fraisPlateforme,
        decimal cotisations, decimal cfp, decimal vl, decimal fraisVariables,
        decimal totalProduits, decimal totalCharges,
        decimal resultatExploitation, decimal impotSurBenefice, decimal resultatNet,
        decimal tresorerie, decimal creancesClients, decimal apports, decimal dettesUrssaf)
    {
        r.Annee = annee;
        r.PeriodeDebut = start;
        r.PeriodeFin = end;
        r.StatutJuridique = entity.StatutJuridique;
        r.ChiffreAffaires = ca;
        r.AutresProduits = 0;
        r.TotalProduits = totalProduits;
        r.AchatsChargesExternes = depenses;
        r.CotisationsSociales = cotisations;
        r.CFP = cfp;
        r.VersementLiberatoire = vl;
        r.FraisPlateforme = fraisPlateforme;
        r.AutresCharges = fraisVariables;
        r.TotalCharges = totalCharges;
        r.ResultatExploitation = resultatExploitation;
        r.ImpotSurBenefice = impotSurBenefice;
        r.ResultatNet = resultatNet;
        r.TresorerieFinExercice = tresorerie;
        r.CreancesClients = creancesClients;
        r.CapitalApports = apports;
        r.ResultatExercice = resultatNet;
        r.DettesUrssaf = dettesUrssaf;
    }
}
