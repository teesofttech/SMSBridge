using SmsBridge.Abstractions;

namespace SmsBridge.Webhooks;

/// <summary>Parses raw provider webhook payloads into a normalised <see cref="SmsWebhookEvent"/>.</summary>
public interface ISmsWebhookParser
{
    SmsProviderType Provider { get; }

    SmsWebhookEvent Parse(IDictionary<string, string> payload);

    /// <summary>Parses a JSON webhook payload for providers that use structured callbacks.</summary>
    SmsWebhookEvent ParseJson(string json) =>
        throw new SmsBridgeException($"Provider '{Provider}' does not use JSON webhook payloads.");
}
