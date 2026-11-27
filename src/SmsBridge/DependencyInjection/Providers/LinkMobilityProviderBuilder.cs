using Microsoft.Extensions.DependencyInjection;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.LinkMobility;

namespace SmsBridge.DependencyInjection;

public static class LinkMobilityProviderBuilder
{
    public static SmsBridgeBuilder UseLinkMobility(this SmsBridgeBuilder builder, string name,
        Action<LinkMobilityProviderConfig> configure)
    {
        var config = new LinkMobilityProviderConfig();
        configure(config);

        if (string.IsNullOrWhiteSpace(config.Username) || config.Username.Contains(':'))
            throw new SmsBridgeException($"LINK Mobility provider '{name}' requires a username without a colon.");
        if (string.IsNullOrWhiteSpace(config.Password))
            throw new SmsBridgeException($"LINK Mobility provider '{name}' requires a password.");
        if (string.IsNullOrWhiteSpace(config.PlatformId) || string.IsNullOrWhiteSpace(config.PlatformPartnerId))
            throw new SmsBridgeException($"LINK Mobility provider '{name}' requires both platform IDs.");
        if (string.IsNullOrWhiteSpace(config.From))
            throw new SmsBridgeException($"LINK Mobility provider '{name}' requires a sender.");
        if (!Uri.TryCreate(config.BaseUrl, UriKind.Absolute, out var gateway) ||
            gateway.Scheme != Uri.UriSchemeHttps || gateway.AbsolutePath != "/" ||
            gateway.UserInfo.Length != 0 || gateway.Query.Length != 0 || gateway.Fragment.Length != 0)
        {
            throw new SmsBridgeException($"LINK Mobility provider '{name}' requires an HTTPS gateway origin.");
        }

        var options = new LinkMobilityOptions
        {
            Username = config.Username,
            Password = config.Password,
            PlatformId = config.PlatformId,
            PlatformPartnerId = config.PlatformPartnerId,
            From = config.From,
            BaseUrl = gateway.GetLeftPart(UriPartial.Authority)
        };
        builder.Services.AddHttpClient(HttpClientNames.LinkMobility)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        builder.AddProvider(name, services => new LinkMobilitySmsProvider(
            name, options, services.GetRequiredService<IHttpClientFactory>()));
        return builder;
    }
}

public sealed class LinkMobilityProviderConfig
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? PlatformId { get; set; }
    public string? PlatformPartnerId { get; set; }
    public string? From { get; set; }
    public string BaseUrl { get; set; } = LinkMobilityOptions.DefaultBaseUrl;
}
