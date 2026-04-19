namespace FrenchInvoice.Core.Models;

public enum QuoteStatus
{
    Brouillon,
    Envoye,
    Accepte,
    Refuse,
    Expire
}

public class Quote : IEntityScoped
{
    public int Id { get; set; }
    public int EntityId { get; set; }
    public int? PropertyId { get; set; }
    public Property? Property { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateTime DateEmission { get; set; } = DateTime.Today;
    public DateTime DateValidite { get; set; } = DateTime.Today.AddDays(30);
    public QuoteStatus Statut { get; set; } = QuoteStatus.Brouillon;

    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public decimal MontantHT { get; set; }
    public decimal MontantTVA { get; set; }
    public decimal MontantTTC { get; set; }

    public string? Notes { get; set; }
    public string? MentionsLegales { get; set; }
    public string? CheminPdf { get; set; }

    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public List<QuoteLine> Lignes { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class QuoteLine
{
    public int Id { get; set; }
    public int QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public decimal Quantite { get; set; } = 1m;
    public decimal PrixUnitaire { get; set; }
    public decimal TauxTVA { get; set; }
    public decimal MontantHT { get; set; }
}
