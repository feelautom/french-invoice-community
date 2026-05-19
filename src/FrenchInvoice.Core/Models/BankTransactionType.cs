namespace FrenchInvoice.Core.Models;

public enum BankTransactionType
{
    Uncategorized = 0,
    Revenue = 1,
    Expense = 2,
    InternalTransfer = 3,
    PersonalContribution = 4,
    PlatformPayout = 5
}
