namespace FrenchInvoice.Core.Models;

public class Expense : IEntityScoped
{
    public int Id { get; set; }
    public int EntityId { get; set; }
    public int? PropertyId { get; set; }
    public Property? Property { get; set; }
    public DateTime Date { get; set; }
    public decimal Montant { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Fournisseur { get; set; } = string.Empty;
    public string Categorie { get; set; } = string.Empty;
    public string ModeReglement { get; set; } = string.Empty;
    public string? Justificatif { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
