using Microsoft.Extensions.DependencyInjection;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Exotel;

namespace SmsBridge.DependencyInjection;

public static class ExotelProviderBuilder
{
    public static SmsBridgeBuilder UseExotel(this SmsBridgeBuilder builder, string name,
        Action<ExotelProviderConfig> configure)
    {
        var config = new ExotelProviderConfig();
        configure(config);

        if (string.IsNullOrWhiteSpace(config.AccountSid) || config.AccountSid is "." or "..")
            throw new SmsBridgeException($"Exotel provider '{name}' requires an account SID.");
        if (string.IsNullOrWhiteSpace(config.ApiKey) || config.ApiKey.Contains(':'))
            throw new SmsBridgeException($"Exotel provider '{name}' requires an API key without a colon.");
        if (string.IsNullOrWhiteSpace(config.AuthToken))
            throw new SmsBridgeException($"Exotel provider '{name}' requires an API token.");
        if (string.IsNullOrWhiteSpace(config.From))
            throw new SmsBridgeException($"Exotel provider '{name}' requires a sender.");
        if (!Enum.IsDefined(config.Region))
            throw new SmsBridgeException($"Exotel provider '{name}' requires a supported region.");

        var options = new ExotelOptions
        {
            AccountSid = config.AccountSid,
            ApiKey = config.ApiKey,
            AuthToken = config.AuthToken,
            From = config.From,
            Region = config.Region,
            DltEntityId = config.DltEntityId,
            DltTemplateId = config.DltTemplateId
        };
        builder.Services.AddHttpClient(HttpClientNames.Exotel)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        builder.AddProvider(name, services => new ExotelSmsProvider(
            name, options, services.GetRequiredService<IHttpClientFactory>()));
        return builder;
    }
}

public sealed class ExotelProviderConfig
{
    public string? AccountSid { get; set; }
    public string? ApiKey { get; set; }
    public string? AuthToken { get; set; }
    public string? From { get; set; }
    public ExotelRegion Region { get; set; } = ExotelRegion.Singapore;
    public string? DltEntityId { get; set; }
    public string? DltTemplateId { get; set; }
}
