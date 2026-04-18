using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public interface ITenantProvider
{
    int EntityId { get; }
    int UserId { get; }
    UserRole Role { get; }
    bool IsSuperAdmin { get; }
    bool IsAtLeastAdmin { get; }
    bool CanEdit { get; }
    Task InitializeAsync();
}

public class TenantProvider : ITenantProvider
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private bool _initialized;

    public int EntityId { get; private set; }
    public int UserId { get; private set; }
    public UserRole Role { get; private set; } = UserRole.User;
    public bool IsSuperAdmin => Role == UserRole.SuperAdmin;
    public bool IsAtLeastAdmin => Role <= UserRole.Admin;
    public bool CanEdit => Role <= UserRole.Admin;

    public TenantProvider(AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var entityClaim = user.FindFirst("entityId")?.Value;
            var userClaim = user.FindFirst("userId")?.Value;
            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

            if (int.TryParse(entityClaim, out var entityId))
                EntityId = entityId;
            if (int.TryParse(userClaim, out var userId))
                UserId = userId;
            if (Enum.TryParse<UserRole>(roleClaim, out var role))
                Role = role;
        }

        _initialized = true;
    }
}
