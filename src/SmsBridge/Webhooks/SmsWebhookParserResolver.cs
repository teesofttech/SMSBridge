using SmsBridge.Abstractions;

namespace SmsBridge.Webhooks;

/// <summary>Resolves the correct webhook parser for a given provider.</summary>
public sealed class SmsWebhookParserResolver
{
    private readonly IReadOnlyDictionary<SmsProviderType, ISmsWebhookParser> _parsers;

    public SmsWebhookParserResolver(IEnumerable<ISmsWebhookParser> parsers)
    {
        var map = new Dictionary<SmsProviderType, ISmsWebhookParser>();

        // Last registration wins, matching how the DI container resolves a single service,
        // so a parser registered after AddSmsBridge replaces the built-in one.
        foreach (var parser in parsers)
            map[parser.Provider] = parser;

        _parsers = map;
    }

    public ISmsWebhookParser Resolve(SmsProviderType provider)
    {
        if (_parsers.TryGetValue(provider, out var parser))
            return parser;

        throw new SmsBridgeException($"No webhook parser is registered for provider type '{provider}'.");
    }
}
