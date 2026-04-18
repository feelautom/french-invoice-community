namespace FrenchInvoice.Core.Models;

public class UserSettings
{
    public int Id { get; set; }
    public ActivityCategory TypeActivite { get; set; } = ActivityCategory.BNC;
    public DeclarationPeriodicity PeriodiciteDeclaration { get; set; } = DeclarationPeriodicity.Mensuelle;
    public DateTime? DateDebutActivite { get; set; }
    public decimal PlafondCA { get; set; } = 77700m;

    // Entreprise
    public string NomEntreprise { get; set; } = "";
    public string NumeroSiret { get; set; } = "";
    public string TvaIntracommunautaire { get; set; } = "";
    public string Telephone { get; set; } = "";
    public string AdresseSiege { get; set; } = "";

    // Facturation
    public string PrefixeFactures { get; set; } = "FAC-2026-";
    public int ProchainNumeroFacture { get; set; } = 1;
    public string PrefixeDevis { get; set; } = "DEV-2026-";
    public int ProchainNumeroDevis { get; set; } = 1;
    public string MentionsLegales { get; set; } = "Auto-entreprise - TVA non applicable, article 293B du code général des impôts";

    // Fiscalité
    public bool FranchiseTVA { get; set; } = true;
    public decimal TauxTVA { get; set; } = 20m;
    public bool VersementLiberatoire { get; set; } = false;
    public decimal TauxLiberatoire { get; set; } = 2.2m;
    public decimal FraisVariables { get; set; } = 0m;
    public bool BeneficieACRE { get; set; } = false;
}
