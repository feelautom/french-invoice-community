namespace FrenchInvoice.Core.Services;

/// <summary>
/// Implémentation no-op de ISecretProvider pour l'édition Community.
/// Retourne null pour tous les secrets — les PDFs sont générés sans IBAN/BIC.
/// </summary>
public class NullSecretProvider : ISecretProvider
{
    public Task<string?> GetSecretAsync(string siret, string secretName)
        => Task.FromResult<string?>(null);

    public Task<string?> GetAppSecretAsync(string secretName)
        => Task.FromResult<string?>(null);
}
