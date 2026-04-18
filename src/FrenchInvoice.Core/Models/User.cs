namespace FrenchInvoice.Core.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public Entity Entity { get; set; } = null!;
    public UserRole Role { get; set; } = UserRole.Admin;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum UserRole
{
    SuperAdmin,
    Admin,
    User
}
