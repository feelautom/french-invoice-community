namespace FrenchInvoice.Core.Services;

/// <summary>
/// Abstraction pour la récupération de secrets (IBAN, BIC, clés API...).
/// SaaS: implémenté par InfisicalService.
/// Community: implémenté par NullSecretProvider (retourne null).
/// </summary>
public interface ISecretProvider
{
    Task<string?> GetSecretAsync(string siret, string secretName);
    Task<string?> GetAppSecretAsync(string secretName);
}
