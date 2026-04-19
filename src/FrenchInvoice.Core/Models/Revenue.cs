namespace FrenchInvoice.Core.Models;

public class Revenue : IEntityScoped
{
    public int Id { get; set; }
    public int EntityId { get; set; }
    public int? PropertyId { get; set; }
    public Property? Property { get; set; }
    public DateTime Date { get; set; }
    public decimal Montant { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    public string ModePaiement { get; set; } = string.Empty;
    public ActivityCategory Categorie { get; set; } = ActivityCategory.BNC;
    public string? ReferenceFacture { get; set; }
    public string? JustificatifFileName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
