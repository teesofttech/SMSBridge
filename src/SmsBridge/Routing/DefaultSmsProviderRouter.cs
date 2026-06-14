using Microsoft.Extensions.Options;
using SmsBridge.Abstractions;
using SmsBridge.Options;

namespace SmsBridge.Routing;

internal sealed class DefaultSmsProviderRouter : ISmsProviderRouter
{
    private readonly IReadOnlyDictionary<string, ISmsProvider> _providers;
    private readonly SmsBridgeOptions _options;

    public DefaultSmsProviderRouter(
        IEnumerable<ISmsProvider> providers,
        IOptions<SmsBridgeOptions> options)
    {
        _providers = providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        _options = options.Value;
    }

    public ISmsProvider Resolve(SmsRoutingContext context)
    {
        var providerName = context.ExplicitProvider ?? _options.DefaultProvider;

        if (string.IsNullOrWhiteSpace(providerName))
            throw new SmsBridgeException(
                "No provider could be resolved. Set 'SmsBridge:DefaultProvider' in configuration or supply a provider name on the message.");

        if (!_providers.TryGetValue(providerName, out var provider))
            throw new SmsBridgeException(
                $"Provider '{providerName}' is not registered. Registered providers: [{string.Join(", ", _providers.Keys)}].");

        return provider;
    }
}
