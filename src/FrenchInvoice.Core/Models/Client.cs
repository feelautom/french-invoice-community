namespace FrenchInvoice.Core.Models;

public class Client : IEntityScoped
{
    public int Id { get; set; }
    public int EntityId { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Adresse { get; set; } = string.Empty;
    public string CodePostal { get; set; } = string.Empty;
    public string Ville { get; set; } = string.Empty;
    public string Pays { get; set; } = "France";
    public string CodePays { get; set; } = "FR";
    public string? Siret { get; set; }
    public string? TvaIntracommunautaire { get; set; }
    public int? SiretDataId { get; set; }
    public SiretData? SiretData { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
