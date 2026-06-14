using SmsBridge.Abstractions;

namespace SmsBridge.Webhooks;

/// <summary>Parses raw provider webhook payloads into a normalised <see cref="SmsWebhookEvent"/>.</summary>
public interface ISmsWebhookParser
{
    SmsProviderType Provider { get; }

    SmsWebhookEvent Parse(IDictionary<string, string> payload);
}
