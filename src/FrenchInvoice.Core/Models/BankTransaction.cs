namespace FrenchInvoice.Core.Models;

public class BankTransaction : IEntityScoped
{
    public int Id { get; set; }
    public int EntityId { get; set; }
    public int? PropertyId { get; set; }
    public Property? Property { get; set; }
    public DateTime Date { get; set; }
    public string Libelle { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public decimal? Solde { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public BankTransactionType Type { get; set; } = BankTransactionType.Uncategorized;
    [Obsolete("Utiliser Type a la place. Conserve temporairement pour double-ecriture.")]
    public bool Categorise { get; set; }
    [Obsolete("Utiliser Type == PersonalContribution. Conserve temporairement pour double-ecriture.")]
    public bool IsApportPersonnel { get; set; }
    public int? RevenueId { get; set; }
    public Revenue? Revenue { get; set; }
    public int? ExpenseId { get; set; }
    public Expense? Expense { get; set; }
}
