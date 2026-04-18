namespace FrenchInvoice.Core.Services;

public enum AppEdition { Community, SaaS }

public interface IEditionProvider
{
    AppEdition Edition { get; }
    bool IsCommunity { get; }
    bool IsSaaS { get; }
}

public class EditionProvider : IEditionProvider
{
    public AppEdition Edition { get; }
    public bool IsCommunity => Edition == AppEdition.Community;
    public bool IsSaaS => Edition == AppEdition.SaaS;

    public EditionProvider(IConfiguration configuration)
    {
        var editionStr = configuration["Edition"] ?? "SaaS";
        Edition = Enum.TryParse<AppEdition>(editionStr, ignoreCase: true, out var edition)
            ? edition
            : AppEdition.SaaS;
    }

    public EditionProvider(string edition)
    {
        Edition = Enum.TryParse<AppEdition>(edition, ignoreCase: true, out var e) ? e : AppEdition.Community;
    }
}
