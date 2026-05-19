namespace FrenchInvoice.Core.Models;

public class ExerciseClosure : IEntityScoped
{
    public int Id { get; set; }
    public int EntityId { get; set; }

    public LegalStatus StatutJuridique { get; set; }
    public DateTime DateDebut { get; set; }
    public DateTime DateFin { get; set; }
    public DateTime ClosedAt { get; set; } = DateTime.UtcNow;

    // Snapshot des parametres fiscaux au moment de la cloture
    public decimal TauxCotisation { get; set; }
    public decimal TauxCFP { get; set; }
    public decimal TauxLiberatoire { get; set; }
    public bool VersementLiberatoire { get; set; }
    public bool FranchiseTVA { get; set; }
    public decimal TauxTVA { get; set; }
    public decimal PlafondCA { get; set; }
    public ActivityCategory TypeActivite { get; set; }

    // Totaux geles
    public decimal TotalCA { get; set; }
    public decimal TotalDepenses { get; set; }
    public decimal TotalCotisations { get; set; }
    public decimal TotalCFP { get; set; }
    public decimal TotalVL { get; set; }
    public decimal BeneficeNet { get; set; }

    public string? Notes { get; set; }
}
