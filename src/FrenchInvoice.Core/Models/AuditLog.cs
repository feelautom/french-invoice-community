namespace FrenchInvoice.Core.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int? EntityId { get; set; }
    public int? UserId { get; set; }
    public string Username { get; set; } = "";
    public string Action { get; set; } = "";
    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
