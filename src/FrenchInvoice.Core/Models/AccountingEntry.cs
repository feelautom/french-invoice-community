namespace FrenchInvoice.Core.Models;

public enum AccountingEntryType
{
    Recette,
    Depense
}

public class AccountingEntry : IEntityScoped
{
    public int Id { get; set; }
    public int EntityId { get; set; }
    public long SequenceNumber { get; set; }
    public AccountingEntryType EntryType { get; set; }
    public int? RevenueId { get; set; }
    public Revenue? Revenue { get; set; }
    public int? ExpenseId { get; set; }
    public Expense? Expense { get; set; }
    public DateTime Date { get; set; }
    public decimal Montant { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Tiers { get; set; } = string.Empty;
    public string PreviousHash { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
