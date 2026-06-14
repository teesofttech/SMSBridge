using SmsBridge.Abstractions;

namespace SmsBridge.Webhooks;

/// <summary>Resolves the correct webhook parser for a given provider.</summary>
public sealed class SmsWebhookParserResolver
{
    private readonly IReadOnlyDictionary<SmsProviderType, ISmsWebhookParser> _parsers;

    public SmsWebhookParserResolver(IEnumerable<ISmsWebhookParser> parsers) =>
        _parsers = parsers.ToDictionary(p => p.Provider);

    public ISmsWebhookParser Resolve(SmsProviderType provider)
    {
        if (_parsers.TryGetValue(provider, out var parser))
            return parser;

        throw new SmsBridgeException($"No webhook parser is registered for provider type '{provider}'.");
    }
}
