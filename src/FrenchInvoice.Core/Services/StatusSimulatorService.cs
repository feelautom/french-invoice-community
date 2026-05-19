using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public class StatusComparison
{
    public List<StatusSimulationResult> Resultats { get; set; } = new();
    public LegalStatus StatutOptimal { get; set; }
    public string Explication { get; set; } = string.Empty;
}

public class StatusSimulationResult
{
    public LegalStatus Statut { get; set; }
    public decimal CA { get; set; }
    public decimal Depenses { get; set; }
    public decimal Cotisations { get; set; }
    public decimal CFP { get; set; }
    public decimal VersementLiberatoire { get; set; }
    public decimal IR { get; set; }
    public decimal IS { get; set; }
    public decimal TotalCharges { get; set; }
    public decimal RevenuNet { get; set; }
    public bool PlafondDepasse { get; set; }
    public List<string> Alertes { get; set; } = new();
}

public class ProjectionResult
{
    public decimal CAProjecteAnnuel { get; set; }
    public decimal TendanceMensuelle { get; set; }
    public List<decimal> CAProjecteMensuel { get; set; } = new();
    public decimal ConfidenceScore { get; set; }
}

public class StatusSimulatorService
{
    private readonly IncomeTaxService _taxService;

    public StatusSimulatorService(IncomeTaxService taxService)
    {
        _taxService = taxService;
    }

    public StatusComparison Compare(decimal caAnnuel, decimal depensesAnnuelles,
        ActivityCategory activite, decimal partsFiscales, bool acre, DateTime? dateDebutActivite)
    {
        var statuts = new[] { LegalStatus.MicroEntreprise, LegalStatus.EIClassique, LegalStatus.EURL, LegalStatus.SASU };
        var resultats = new List<StatusSimulationResult>();

        foreach (var statut in statuts)
        {
            var result = SimulerStatut(statut, caAnnuel, depensesAnnuelles, activite, partsFiscales, acre, dateDebutActivite);
            resultats.Add(result);
        }

        var eligibles = resultats.Where(r => !r.PlafondDepasse).ToList();
        var optimal = (eligibles.Any() ? eligibles : resultats)
            .OrderByDescending(r => r.RevenuNet)
            .First();

        var explication = GenererExplication(optimal, resultats);

        return new StatusComparison
        {
            Resultats = resultats,
            StatutOptimal = optimal.Statut,
            Explication = explication
        };
    }

    public ProjectionResult Projeter(List<decimal> caMensuels, int moisAProjecter)
    {
        if (caMensuels.Count == 0)
            return new ProjectionResult();

        // Moyenne mobile ponderee (mois recent = poids plus fort)
        var count = Math.Min(caMensuels.Count, 6);
        var recent = caMensuels.Skip(caMensuels.Count - count).ToList();

        decimal sommePoidsCA = 0;
        decimal sommePoids = 0;
        for (int i = 0; i < recent.Count; i++)
        {
            var poids = 0.5m + (0.5m * i / Math.Max(1, recent.Count - 1));
            sommePoidsCA += recent[i] * poids;
            sommePoids += poids;
        }
        var moyennePonderee = sommePoids > 0 ? sommePoidsCA / sommePoids : 0;

        // Projections
        var projections = new List<decimal>();
        for (int i = 0; i < moisAProjecter; i++)
            projections.Add(Math.Round(moyennePonderee, 2));

        // CA annuel projete = mois passes + projections
        var caTotal = caMensuels.Sum() + projections.Sum();

        // Confiance = 1 - (ecart-type / moyenne)
        var moyenne = caMensuels.Average();
        var variance = caMensuels.Count > 1
            ? caMensuels.Select(x => (x - moyenne) * (x - moyenne)).Sum() / (caMensuels.Count - 1)
            : 0;
        var ecartType = (decimal)Math.Sqrt((double)variance);
        var confidence = moyenne > 0 ? Math.Max(0, Math.Min(1, 1m - ecartType / moyenne)) : 0;

        return new ProjectionResult
        {
            CAProjecteAnnuel = Math.Round(caTotal, 2),
            TendanceMensuelle = Math.Round(moyennePonderee, 2),
            CAProjecteMensuel = projections,
            ConfidenceScore = Math.Round(confidence, 2)
        };
    }

    private StatusSimulationResult SimulerStatut(LegalStatus statut, decimal ca, decimal depenses,
        ActivityCategory activite, decimal partsFiscales, bool acre, DateTime? dateDebutActivite)
    {
        var defaults = TaxRuleEngine.GetDefaults(statut, activite);
        var assiette = TaxRuleEngine.GetAssietteCotisations(statut, ca, depenses);
        var cotisations = TaxRuleEngine.CalculerCotisations(statut, assiette, defaults.TauxCotisation, acre, dateDebutActivite);
        var cfp = TaxRuleEngine.CalculerCFP(statut, assiette, defaults.TauxCFP);

        // VL uniquement micro
        decimal vl = 0;
        if (statut == LegalStatus.MicroEntreprise)
            vl = Math.Round(ca * defaults.TauxLiberatoire / 100m, 2);

        // IR
        decimal ir = 0;
        decimal isVal = 0;
        if (TaxRuleEngine.EstSoumisIS(statut))
        {
            // SASU : IS sur benefice, IR sur remuneration (50% du benefice en simplification)
            var benefice = Math.Max(0, ca - depenses - cotisations - cfp);
            var isResult = _taxService.CalculerIS(benefice);
            isVal = isResult.TotalIS;

            // Remuneration simulee = 50% benefice, reste en dividendes flat tax 30%
            var remuneration = benefice * 0.5m;
            var dividendes = benefice - remuneration - isVal;
            var irResult = _taxService.Calculer(remuneration, activite, partsFiscales, false, 0, statut, 0);
            ir = irResult.ImpotNet;
            if (dividendes > 0)
                ir += Math.Round(dividendes * 0.30m, 2); // flat tax
        }
        else
        {
            var irResult = _taxService.Calculer(ca, activite, partsFiscales, false, 0, statut, depenses);
            ir = irResult.ImpotNet;
        }

        var totalCharges = cotisations + cfp + vl + ir + isVal;
        var revenuNet = ca - depenses - cotisations - cfp - vl - ir - isVal;
        if (statut == LegalStatus.MicroEntreprise)
            revenuNet = ca - cotisations - cfp - vl - ir; // Micro : pas de deduction depenses

        var plafond = TaxRuleEngine.PlafondCA(statut, activite);
        var plafondDepasse = plafond > 0 && ca > plafond;

        var alertes = new List<string>();
        if (plafondDepasse)
            alertes.Add($"CA depasse le plafond micro de {(ca - plafond):N0} EUR - changement obligatoire");
        if (statut == LegalStatus.MicroEntreprise && plafond > 0 && ca > plafond * 0.9m && !plafondDepasse)
            alertes.Add("Proche du plafond micro (> 90%)");

        return new StatusSimulationResult
        {
            Statut = statut,
            CA = ca,
            Depenses = depenses,
            Cotisations = cotisations,
            CFP = cfp,
            VersementLiberatoire = vl,
            IR = ir,
            IS = isVal,
            TotalCharges = totalCharges,
            RevenuNet = revenuNet,
            PlafondDepasse = plafondDepasse,
            Alertes = alertes
        };
    }

    private string GenererExplication(StatusSimulationResult optimal, List<StatusSimulationResult> tous)
    {
        var deuxieme = tous.Where(r => r.Statut != optimal.Statut && !r.PlafondDepasse)
            .OrderByDescending(r => r.RevenuNet).FirstOrDefault();

        var label = TaxRuleEngine.GetLibelleStatut(optimal.Statut);
        if (deuxieme == null)
            return $"Le statut {label} est le plus avantageux pour votre situation.";

        var ecart = optimal.RevenuNet - deuxieme.RevenuNet;
        var label2 = TaxRuleEngine.GetLibelleStatut(deuxieme.Statut);
        return $"Le statut {label} vous fait gagner {ecart:N0} EUR/an par rapport a {label2}.";
    }
}
