namespace FrenchInvoice.Core.Models;

public class PayoutRecord : IEntityScoped
{
    public int Id { get; set; }
    public int EntityId { get; set; }
    public string Platform { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public DateTime Date { get; set; }
    public decimal MontantBrut { get; set; }
    public decimal Frais { get; set; }
    public decimal MontantNet { get; set; }
    public string? StatementDescription { get; set; }
    public string Status { get; set; } = "";

    /// <summary>
    /// Lien vers la transaction bancaire rapprochée (null si pas encore rapproché)
    /// </summary>
    public int? BankTransactionId { get; set; }
    public BankTransaction? BankTransaction { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
