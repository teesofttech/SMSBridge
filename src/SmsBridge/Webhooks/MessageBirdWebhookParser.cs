using SmsBridge.Abstractions;

namespace SmsBridge.Webhooks;

/// <summary>Planned MessageBird webhook parser — not yet implemented.</summary>
public sealed class MessageBirdWebhookParser : ISmsWebhookParser
{
    public SmsProviderType Provider => SmsProviderType.MessageBird;

    public SmsWebhookEvent Parse(IDictionary<string, string> payload) =>
        throw new SmsBridgeException("The MessageBird webhook parser is planned but not yet implemented.");
}
