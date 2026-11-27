using Microsoft.Extensions.DependencyInjection;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Esendex;

namespace SmsBridge.DependencyInjection;

public static class EsendexProviderBuilder
{
    public static SmsBridgeBuilder UseEsendex(this SmsBridgeBuilder builder, string name,
        Action<EsendexProviderConfig> configure)
    {
        var config = new EsendexProviderConfig();
        configure(config);

        if (string.IsNullOrWhiteSpace(config.Username) || config.Username.Contains(':'))
            throw new SmsBridgeException($"Esendex provider '{name}' requires a username without a colon.");
        if (string.IsNullOrWhiteSpace(config.Password))
            throw new SmsBridgeException($"Esendex provider '{name}' requires an API password.");
        if (string.IsNullOrWhiteSpace(config.AccountReference))
            throw new SmsBridgeException($"Esendex provider '{name}' requires an account reference.");

        var options = new EsendexOptions
        {
            Username = config.Username,
            Password = config.Password,
            AccountReference = config.AccountReference,
            From = config.From
        };
        builder.Services.AddHttpClient(HttpClientNames.Esendex)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        builder.AddProvider(name, services => new EsendexSmsProvider(
            name, options, services.GetRequiredService<IHttpClientFactory>()));
        return builder;
    }
}

public sealed class EsendexProviderConfig
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? AccountReference { get; set; }
    public string? From { get; set; }
}
