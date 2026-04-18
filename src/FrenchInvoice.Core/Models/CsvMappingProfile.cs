namespace FrenchInvoice.Core.Models;

public enum CsvProfileType
{
    Banque,
    Plateforme
}

public class CsvMappingProfile : IEntityScoped
{
    public int Id { get; set; }
    public int EntityId { get; set; }
    public string Nom { get; set; } = string.Empty;

    // Type de profil : Banque (→ BankTransaction) ou Plateforme (→ Revenue)
    public CsvProfileType ProfileType { get; set; } = CsvProfileType.Banque;

    // Séparateur CSV
    public string Separator { get; set; } = ";";

    // Ligne d'en-tête (1-based, 0 = pas d'en-tête)
    public int HeaderRow { get; set; } = 1;

    // Format de date (ex: "yyyy-MM-dd", "dd/MM/yyyy")
    public string DateFormat { get; set; } = string.Empty;

    // Indices de colonnes communs (0-based)
    public int DateColumn { get; set; }
    public int LibelleColumn { get; set; } = 1;
    public int? MontantColumn { get; set; } = 2;
    public int? DebitColumn { get; set; }
    public int? CreditColumn { get; set; }
    public int? SoldeColumn { get; set; }

    // Colonnes spécifiques aux plateformes de paiement (type Plateforme)
    public int? ClientColumn { get; set; }
    public int? ModePaiementColumn { get; set; }
    public int? ReferenceColumn { get; set; }
    public int? FraisColumn { get; set; }

    // Profil système = pré-configuré, non modifiable/supprimable par l'utilisateur
    public bool IsSystem { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
