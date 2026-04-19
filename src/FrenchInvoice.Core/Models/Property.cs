namespace FrenchInvoice.Core.Models;

/// <summary>
/// Sous-entité / projet / activité au sein d'une Entity.
/// Permet de segmenter clients, factures, revenus et dépenses par source d'activité
/// (ex: T-IA Connect, NetSwitch, FrenchInvoice...) tout en gardant une seule entité fiscale.
/// </summary>
public class Property : IEntityScoped
{
    public int Id { get; set; }
    public int EntityId { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Couleur { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
