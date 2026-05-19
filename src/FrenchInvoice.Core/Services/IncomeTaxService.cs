namespace FrenchInvoice.Core.Services;

using FrenchInvoice.Core.Models;

public class TrancheDetail
{
    public decimal Plancher { get; set; }
    public decimal Plafond { get; set; }
    public decimal Taux { get; set; }
    public decimal MontantImposable { get; set; }
    public decimal Impot { get; set; }
}

public class IncomeTaxResult
{
    public decimal CA { get; set; }
    public decimal AbattementPourcent { get; set; }
    public decimal Abattement { get; set; }
    public decimal RevenuImposable { get; set; }
    public decimal NombreParts { get; set; }
    public decimal QuotientFamilial { get; set; }
    public decimal ImpotBrut { get; set; }
    public decimal ImpotNet { get; set; }
    public decimal TauxMoyen { get; set; }
    public decimal TauxMarginal { get; set; }
    public decimal VersementLiberatoire { get; set; }
    public decimal EconomieVL { get; set; }
    public List<TrancheDetail> Tranches { get; set; } = new();
}

public class ISResult
{
    public decimal Benefice { get; set; }
    public decimal TauxReduit { get; set; }
    public decimal MontantTauxReduit { get; set; }
    public decimal TauxNormal { get; set; }
    public decimal MontantTauxNormal { get; set; }
    public decimal TotalIS { get; set; }
}

public class IncomeTaxService
{
    // Bareme IR 2025 (revenus 2024)
    private static readonly (decimal plafond, decimal taux)[] Bareme =
    {
        (11_497m, 0m),
        (29_315m, 0.11m),
        (83_823m, 0.30m),
        (180_294m, 0.41m),
        (decimal.MaxValue, 0.45m)
    };

    // Seuil IS taux reduit
    private const decimal SeuilIS = 42_500m;
    private const decimal TauxISReduit = 15m;
    private const decimal TauxISNormal = 25m;

    public static decimal GetAbattement(ActivityCategory categorie) => categorie switch
    {
        ActivityCategory.BICVente => 71m,
        ActivityCategory.BICServices => 50m,
        ActivityCategory.BNC => 34m,
        _ => 34m
    };

    public ISResult CalculerIS(decimal benefice)
    {
        var base_ = Math.Max(0, benefice);
        var partReduit = Math.Min(base_, SeuilIS);
        var partNormal = Math.Max(0, base_ - SeuilIS);
        var montantReduit = Math.Round(partReduit * TauxISReduit / 100m, 2);
        var montantNormal = Math.Round(partNormal * TauxISNormal / 100m, 2);

        return new ISResult
        {
            Benefice = base_,
            TauxReduit = TauxISReduit,
            MontantTauxReduit = montantReduit,
            TauxNormal = TauxISNormal,
            MontantTauxNormal = montantNormal,
            TotalIS = montantReduit + montantNormal
        };
    }

    public IncomeTaxResult Calculer(decimal ca, Entity settings)
    {
        return Calculer(ca, settings.TypeActivite, settings.NombrePartsFiscales,
            settings.VersementLiberatoire, settings.TauxLiberatoire, settings.StatutJuridique);
    }

    public IncomeTaxResult Calculer(decimal ca, Entity settings, decimal depenses)
    {
        return Calculer(ca, settings.TypeActivite, settings.NombrePartsFiscales,
            settings.VersementLiberatoire, settings.TauxLiberatoire, settings.StatutJuridique, depenses);
    }

    public IncomeTaxResult Calculer(decimal ca, ActivityCategory categorie,
        decimal nombreParts, bool versementLiberatoire, decimal tauxLiberatoire,
        LegalStatus statut = LegalStatus.MicroEntreprise, decimal depenses = 0)
    {
        decimal revenuImposable;
        decimal abattementPct = 0;
        decimal abattement = 0;

        if (TaxRuleEngine.AbattementForfaitaire(statut))
        {
            // Micro : abattement forfaitaire
            abattementPct = GetAbattement(categorie);
            abattement = Math.Round(ca * abattementPct / 100m, 2);
            if (abattement < 305m && ca > 0)
                abattement = Math.Min(305m, ca);
            revenuImposable = Math.Max(0, ca - abattement);
        }
        else
        {
            // EI/EURL/SASU : IR sur benefice reel (CA - depenses)
            revenuImposable = Math.Max(0, ca - depenses);
            versementLiberatoire = false; // VL indisponible hors micro
        }

        var parts = Math.Max(1m, nombreParts);
        var quotient = Math.Round(revenuImposable / parts, 2);

        var tranches = new List<TrancheDetail>();
        decimal impotParPart = 0;
        decimal tauxMarginal = 0;
        decimal plancher = 0;

        foreach (var (plafond, taux) in Bareme)
        {
            if (quotient <= plancher)
            {
                tranches.Add(new TrancheDetail
                {
                    Plancher = plancher,
                    Plafond = plafond == decimal.MaxValue ? plancher : plafond,
                    Taux = taux * 100m,
                    MontantImposable = 0,
                    Impot = 0
                });
                plancher = plafond;
                continue;
            }

            var montantDansTranche = Math.Min(quotient, plafond) - plancher;
            var impotTranche = Math.Round(montantDansTranche * taux, 2);
            impotParPart += impotTranche;

            if (montantDansTranche > 0)
                tauxMarginal = taux * 100m;

            tranches.Add(new TrancheDetail
            {
                Plancher = plancher,
                Plafond = plafond == decimal.MaxValue ? quotient : plafond,
                Taux = taux * 100m,
                MontantImposable = montantDansTranche,
                Impot = impotTranche
            });

            plancher = plafond;
        }

        var impotBrut = Math.Round(impotParPart * parts, 0);
        var impotNet = Math.Max(0, impotBrut);
        var tauxMoyen = revenuImposable > 0 ? Math.Round(impotNet / revenuImposable * 100m, 2) : 0;

        var vlMontant = versementLiberatoire ? Math.Round(ca * tauxLiberatoire / 100m, 2) : 0;
        var economieVL = versementLiberatoire ? impotNet - vlMontant : 0;

        return new IncomeTaxResult
        {
            CA = ca,
            AbattementPourcent = abattementPct,
            Abattement = abattement,
            RevenuImposable = revenuImposable,
            NombreParts = parts,
            QuotientFamilial = quotient,
            ImpotBrut = impotBrut,
            ImpotNet = impotNet,
            TauxMoyen = tauxMoyen,
            TauxMarginal = tauxMarginal,
            VersementLiberatoire = vlMontant,
            EconomieVL = economieVL,
            Tranches = tranches
        };
    }
}
