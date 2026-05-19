namespace FrenchInvoice.Core.Models;

public class AnnualReport : IEntityScoped
{
    public int Id { get; set; }
    public int EntityId { get; set; }
    public int? ExerciseClosureId { get; set; }

    // Periode
    public int Annee { get; set; }
    public DateTime PeriodeDebut { get; set; }
    public DateTime PeriodeFin { get; set; }
    public LegalStatus StatutJuridique { get; set; }

    // Compte de resultat
    public decimal ChiffreAffaires { get; set; }
    public decimal AutresProduits { get; set; }
    public decimal TotalProduits { get; set; }

    public decimal AchatsChargesExternes { get; set; }
    public decimal CotisationsSociales { get; set; }
    public decimal CFP { get; set; }
    public decimal VersementLiberatoire { get; set; }
    public decimal FraisPlateforme { get; set; }
    public decimal AutresCharges { get; set; }
    public decimal TotalCharges { get; set; }

    public decimal ResultatExploitation { get; set; }
    public decimal ImpotSurBenefice { get; set; }
    public decimal ResultatNet { get; set; }

    // Bilan simplifie (Actif)
    public decimal TresorerieFinExercice { get; set; }
    public decimal CreancesClients { get; set; }

    // Bilan simplifie (Passif)
    public decimal CapitalApports { get; set; }
    public decimal ResultatExercice { get; set; }
    public decimal DettesUrssaf { get; set; }

    // Metadonnees
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string? PdfPath { get; set; }
}
