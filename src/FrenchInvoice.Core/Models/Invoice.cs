namespace FrenchInvoice.Core.Models;

public enum InvoiceStatus
{
    Brouillon,
    Envoyee,
    Payee,
    Annulee
}

public enum InvoiceType
{
    Facture = 380,
    Avoir = 381,
    Acompte = 386,
    AutoFacture = 389
}

/// <summary>
/// Cadre de facturation (BT-23) — requis par la réforme e-invoicing française sept. 2026.
/// Détermine le contexte de facturation pour le routage PDP/PPF.
/// </summary>
public enum CadreFacturation
{
    /// <summary>B2B domestique (France métropolitaine)</summary>
    A1,
    /// <summary>B2G — Marché public (Chorus Pro)</summary>
    A2,
    /// <summary>B2B avec autoliquidation de la TVA</summary>
    A3,
    /// <summary>Sous-traitance BTP (autoliquidation art. 283-2 nonies CGI)</summary>
    A4,
    /// <summary>Facture à l'exportation (hors UE)</summary>
    A7,
    /// <summary>Facture intracommunautaire (UE)</summary>
    A8,
    /// <summary>Facture avec les DOM-TOM</summary>
    A9
}

public class Invoice : IEntityScoped
{
    public int Id { get; set; }
    public int EntityId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateTime DateEmission { get; set; } = DateTime.Today;
    public DateTime DateEcheance { get; set; } = DateTime.Today.AddDays(30);
    public InvoiceStatus Statut { get; set; } = InvoiceStatus.Brouillon;
    public InvoiceType TypeFacture { get; set; } = InvoiceType.Facture;

    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public decimal MontantHT { get; set; }
    public decimal MontantTVA { get; set; }
    public decimal MontantTTC { get; set; }

    public string? Notes { get; set; }
    public string? MentionsLegales { get; set; }
    public string? CheminPdf { get; set; }
    public string? NumeroCommande { get; set; }
    public DateTime? DebutPeriode { get; set; }
    public DateTime? FinPeriode { get; set; }

    /// <summary>Cadre de facturation (BT-23) — contexte PDP/PPF</summary>
    public CadreFacturation? Cadre { get; set; }
    /// <summary>Référence de contrat (BT-12)</summary>
    public string? NumeroContrat { get; set; }

    public int? RevenueId { get; set; }
    public Revenue? Revenue { get; set; }

    public List<InvoiceLine> Lignes { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class InvoiceLine
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public decimal Quantite { get; set; } = 1m;
    public decimal PrixUnitaire { get; set; }
    public decimal TauxTVA { get; set; }
    public decimal MontantHT { get; set; }
    /// <summary>Code article / identifiant produit (BT-157)</summary>
    public string? CodeProduit { get; set; }
}
