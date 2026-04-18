namespace FrenchInvoice.Core.Models;

public class Declaration : IEntityScoped
{
    public int Id { get; set; }
    public int EntityId { get; set; }
    public string Periode { get; set; } = string.Empty;
    public DateTime PeriodeDebut { get; set; }
    public DateTime PeriodeFin { get; set; }
    public DateTime DateLimite { get; set; }
    public decimal MontantCA { get; set; }
    public decimal MontantCotisations { get; set; }
    public decimal TauxApplique { get; set; }
    public DeclarationStatut Statut { get; set; } = DeclarationStatut.AFaire;
    public DateTime? DateDeclaration { get; set; }
    public DateTime? DatePaiement { get; set; }
    public string? JustificatifFileName { get; set; }
}

public enum DeclarationStatut
{
    AFaire,
    Declaree,
    Payee
}
