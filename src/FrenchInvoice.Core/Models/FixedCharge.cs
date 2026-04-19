namespace FrenchInvoice.Core.Models;

public class FixedCharge : IEntityScoped
{
    public int Id { get; set; }
    public int EntityId { get; set; }
    public int? PropertyId { get; set; }
    public Property? Property { get; set; }
    public string Nom { get; set; } = "";
    public decimal Montant { get; set; }
    public bool Active { get; set; } = true;
}
