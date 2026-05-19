using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public record TaxDefaults(
    decimal TauxCotisation,
    decimal TauxCFP,
    decimal TauxTVA,
    bool FranchiseTVA,
    decimal PlafondCA,
    decimal TauxLiberatoire
);

public static class TaxRuleEngine
{
    public static TaxDefaults GetDefaults(LegalStatus status, ActivityCategory activite)
    {
        return status switch
        {
            LegalStatus.MicroEntreprise => new TaxDefaults(
                TauxCotisation: GetTauxCotisationMicro(activite),
                TauxCFP: 0.2m,
                TauxTVA: 20m,
                FranchiseTVA: true,
                PlafondCA: GetPlafondMicro(activite),
                TauxLiberatoire: GetTauxLiberatoire(activite)
            ),
            LegalStatus.EIClassique => new TaxDefaults(
                TauxCotisation: 45m,
                TauxCFP: 0.25m,
                TauxTVA: 20m,
                FranchiseTVA: false,
                PlafondCA: 0m,
                TauxLiberatoire: 0m
            ),
            LegalStatus.EURL => new TaxDefaults(
                TauxCotisation: 45m,
                TauxCFP: 0.25m,
                TauxTVA: 20m,
                FranchiseTVA: false,
                PlafondCA: 0m,
                TauxLiberatoire: 0m
            ),
            LegalStatus.SASU => new TaxDefaults(
                TauxCotisation: 82m,
                TauxCFP: 0.25m,
                TauxTVA: 20m,
                FranchiseTVA: false,
                PlafondCA: 0m,
                TauxLiberatoire: 0m
            ),
            _ => GetDefaults(LegalStatus.MicroEntreprise, activite)
        };
    }

    /// <summary>
    /// Calcule les cotisations selon le statut.
    /// Micro : sur CA brut. EI/EURL : sur benefice (CA - depenses). SASU : sur salaire brut.
    /// </summary>
    public static decimal CalculerCotisations(LegalStatus status, decimal assiette, decimal tauxCotisation, bool acre, DateTime? dateDebutActivite)
    {
        var taux = GetTauxEffectif(status, tauxCotisation, acre, dateDebutActivite);
        return Math.Round(assiette * taux / 100m, 2);
    }

    public static decimal CalculerCFP(LegalStatus status, decimal assiette, decimal tauxCFP)
    {
        return Math.Round(assiette * tauxCFP / 100m, 2);
    }

    public static decimal GetTauxEffectif(LegalStatus status, decimal tauxCotisation, bool acre, DateTime? dateDebutActivite)
    {
        var taux = tauxCotisation;
        // ACRE : reduction uniquement en micro-entreprise (premiere annee)
        if (acre && status == LegalStatus.MicroEntreprise && dateDebutActivite.HasValue)
        {
            var finACRE = dateDebutActivite.Value.AddYears(1);
            if (DateTime.Today < finACRE)
                taux /= 2m;
        }
        return taux;
    }

    public static bool VersementLiberatoireDisponible(LegalStatus status)
        => status == LegalStatus.MicroEntreprise;

    public static bool AbattementForfaitaire(LegalStatus status)
        => status == LegalStatus.MicroEntreprise;

    public static bool EstSoumisIS(LegalStatus status)
        => status == LegalStatus.SASU;

    /// <summary>
    /// Retourne l'assiette de cotisations selon le statut.
    /// Micro : CA brut. EI/EURL/SASU : benefice (CA - depenses).
    /// </summary>
    public static decimal GetAssietteCotisations(LegalStatus status, decimal ca, decimal depenses)
    {
        return status == LegalStatus.MicroEntreprise ? ca : Math.Max(0, ca - depenses);
    }

    public static decimal PlafondCA(LegalStatus status, ActivityCategory activite)
    {
        if (status != LegalStatus.MicroEntreprise) return 0m;
        return GetPlafondMicro(activite);
    }

    public static string GetMentionsLegales(LegalStatus status, Entity entity)
    {
        if (status == LegalStatus.MicroEntreprise && entity.FranchiseTVA)
            return "TVA non applicable, article 293B du code general des impots";

        if (!entity.FranchiseTVA && !string.IsNullOrEmpty(entity.TvaIntracommunautaire))
            return $"TVA intracommunautaire : {entity.TvaIntracommunautaire}";

        return status switch
        {
            LegalStatus.MicroEntreprise => "Auto-entreprise",
            LegalStatus.EIClassique => "Entreprise individuelle",
            LegalStatus.EURL => "EURL",
            LegalStatus.SASU => "SASU",
            _ => ""
        };
    }

    public static string GetLibelleStatut(LegalStatus status) => status switch
    {
        LegalStatus.MicroEntreprise => "Micro-entreprise",
        LegalStatus.EIClassique => "EI classique",
        LegalStatus.EURL => "EURL",
        LegalStatus.SASU => "SASU",
        _ => "Micro-entreprise"
    };

    // --- Helpers prives ---

    private static decimal GetTauxCotisationMicro(ActivityCategory activite) => activite switch
    {
        ActivityCategory.BICVente => 12.3m,
        ActivityCategory.BICServices => 21.2m,
        ActivityCategory.BNC => 21.1m,
        _ => 21.1m
    };

    private static decimal GetPlafondMicro(ActivityCategory activite) => activite switch
    {
        ActivityCategory.BICVente => 188700m,
        _ => 77700m
    };

    private static decimal GetTauxLiberatoire(ActivityCategory activite) => activite switch
    {
        ActivityCategory.BICVente => 1.0m,
        ActivityCategory.BICServices => 1.7m,
        ActivityCategory.BNC => 2.2m,
        _ => 2.2m
    };
}
