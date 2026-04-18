using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

/// <summary>
/// AuthStateProvider pour le mode Community : toujours authentifie en tant qu'Admin de l'entite 1.
/// Pas de login, pas de token, pas de ProtectedLocalStorage.
/// Herite directement de AuthenticationStateProvider (pas de CustomAuthStateProvider).
/// </summary>
public class CommunityAuthStateProvider : AuthenticationStateProvider
{
    private readonly AuthenticationState _state;

    public CommunityAuthStateProvider()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, nameof(UserRole.Admin)),
            new Claim("userId", "1"),
            new Claim("entityId", "1"),
            new Claim("entityName", "Mon entreprise")
        };
        var identity = new ClaimsIdentity(claims, "Community");
        _state = new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(_state);
}
