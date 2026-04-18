using FrenchInvoice.Core.Models;
using FrenchInvoice.Core.Services;

namespace FrenchInvoice.Tests.Fixtures;

public class TestTenantProvider : ITenantProvider
{
    public int EntityId { get; set; }
    public int UserId { get; set; }
    public UserRole Role { get; set; } = UserRole.Admin;
    public bool IsSuperAdmin => Role == UserRole.SuperAdmin;
    public bool IsAtLeastAdmin => Role <= UserRole.Admin;
    public bool CanEdit => Role <= UserRole.Admin;

    public TestTenantProvider(int entityId = 1, int userId = 1)
    {
        EntityId = entityId;
        UserId = userId;
    }

    public Task InitializeAsync() => Task.CompletedTask;
}
