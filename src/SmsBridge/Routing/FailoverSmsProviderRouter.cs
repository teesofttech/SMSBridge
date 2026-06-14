using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmsBridge.Abstractions;
using SmsBridge.Options;

namespace SmsBridge.Routing;

internal sealed class FailoverSmsProviderRouter : ISmsProviderRouter
{
    private readonly IReadOnlyDictionary<string, ISmsProvider> _providers;
    private readonly SmsBridgeOptions _options;
    private readonly ILogger<FailoverSmsProviderRouter> _logger;

    public FailoverSmsProviderRouter(
        IEnumerable<ISmsProvider> providers,
        IOptions<SmsBridgeOptions> options,
        ILogger<FailoverSmsProviderRouter> logger)
    {
        _providers = providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        _options = options.Value;
        _logger = logger;
    }

    public ISmsProvider Resolve(SmsRoutingContext context)
    {
        var providerName = context.ExplicitProvider ?? _options.DefaultProvider;

        if (string.IsNullOrWhiteSpace(providerName))
            throw new SmsBridgeException(
                "No provider could be resolved. Set 'SmsBridge:DefaultProvider' in configuration.");

        if (!_providers.TryGetValue(providerName, out var provider))
            throw new SmsBridgeException(
                $"Provider '{providerName}' is not registered. Registered: [{string.Join(", ", _providers.Keys)}].");

        return provider;
    }

    public ISmsProvider? ResolveFailover()
    {
        if (!_options.EnableFailover || string.IsNullOrWhiteSpace(_options.FailoverProvider))
            return null;

        if (!_providers.TryGetValue(_options.FailoverProvider, out var provider))
        {
            _logger.LogWarning("Failover provider '{FailoverProvider}' is configured but not registered.", _options.FailoverProvider);
            return null;
        }

        return provider;
    }
}
