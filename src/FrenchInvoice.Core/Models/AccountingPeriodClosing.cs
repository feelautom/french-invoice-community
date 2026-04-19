namespace FrenchInvoice.Core.Models;

public class AccountingPeriodClosing : IEntityScoped
{
    public int Id { get; set; }
    public int EntityId { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime ClosedAt { get; set; } = DateTime.UtcNow;
    public string ClosedBy { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public int EntryCount { get; set; }
    public decimal TotalRecettes { get; set; }
    public decimal TotalDepenses { get; set; }
    public string? Notes { get; set; }
}
