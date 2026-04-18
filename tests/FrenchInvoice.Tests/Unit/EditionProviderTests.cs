using FluentAssertions;
using FrenchInvoice.Core.Services;
using Microsoft.Extensions.Configuration;

namespace FrenchInvoice.Tests.Unit;

public class EditionProviderTests
{
    private static IConfiguration BuildConfig(string? edition)
    {
        var dict = new Dictionary<string, string?>();
        if (edition != null)
            dict["Edition"] = edition;
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public void DefaultEdition_IsSaaS()
    {
        var provider = new EditionProvider(BuildConfig(null));
        provider.Edition.Should().Be(AppEdition.SaaS);
        provider.IsSaaS.Should().BeTrue();
        provider.IsCommunity.Should().BeFalse();
    }

    [Fact]
    public void Edition_Community_Detected()
    {
        var provider = new EditionProvider(BuildConfig("Community"));
        provider.Edition.Should().Be(AppEdition.Community);
        provider.IsCommunity.Should().BeTrue();
        provider.IsSaaS.Should().BeFalse();
    }

    [Fact]
    public void Edition_SaaS_Detected()
    {
        var provider = new EditionProvider(BuildConfig("SaaS"));
        provider.Edition.Should().Be(AppEdition.SaaS);
        provider.IsSaaS.Should().BeTrue();
    }

    [Fact]
    public void Edition_CaseInsensitive()
    {
        var provider = new EditionProvider(BuildConfig("community"));
        provider.IsCommunity.Should().BeTrue();
    }

    [Fact]
    public void Edition_Invalid_DefaultsToSaaS()
    {
        var provider = new EditionProvider(BuildConfig("InvalidValue"));
        provider.IsSaaS.Should().BeTrue();
    }
}
