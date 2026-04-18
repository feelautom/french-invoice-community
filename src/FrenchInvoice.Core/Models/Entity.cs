namespace FrenchInvoice.Core.Models;

public enum ActivityCategory
{
    BICVente,
    BICServices,
    BNC
}

public enum DeclarationPeriodicity
{
    Mensuelle,
    Trimestrielle
}

public class Entity
{
    public int Id { get; set; }
    public string Nom { get; set; } = "";

    // Activité & Fiscalité
    public ActivityCategory TypeActivite { get; set; } = ActivityCategory.BNC;
    public DeclarationPeriodicity PeriodiciteDeclaration { get; set; } = DeclarationPeriodicity.Mensuelle;
    public DateTime? DateDebutActivite { get; set; }
    public decimal PlafondCA { get; set; } = 77700m;

    // Entreprise
    public string NumeroSiret { get; set; } = "";
    public string TvaIntracommunautaire { get; set; } = "";
    public string Telephone { get; set; } = "";
    public string? Email { get; set; }
    public string AdresseSiege { get; set; } = "";
    public string CodePostal { get; set; } = "";
    public string Ville { get; set; } = "";
    public string CodePays { get; set; } = "FR";
    public int? SiretDataId { get; set; }
    public SiretData? SiretData { get; set; }
    public bool Configured { get; set; }
    public string? LicenseKey { get; set; }

    // Facturation
    public string PrefixeFactures { get; set; } = "FAC-2026-";
    public int ProchainNumeroFacture { get; set; } = 1;
    public string PrefixeDevis { get; set; } = "DEV-2026-";
    public int ProchainNumeroDevis { get; set; } = 1;
    public string MentionsLegales { get; set; } = "Auto-entreprise - TVA non applicable, article 293B du code général des impôts";

    // Fiscalité
    public bool FranchiseTVA { get; set; } = true;
    public decimal TauxTVA { get; set; } = 20m;
    public bool VersementLiberatoire { get; set; }
    public decimal TauxLiberatoire { get; set; } = 2.2m;
    public decimal FraisVariables { get; set; }
    public bool BeneficieACRE { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
