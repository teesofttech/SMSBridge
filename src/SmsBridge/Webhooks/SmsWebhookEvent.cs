using SmsBridge.Abstractions;

namespace SmsBridge.Webhooks;

/// <summary>Normalised webhook event from any supported provider.</summary>
public sealed class SmsWebhookEvent
{
    public SmsProviderType Provider { get; init; }

    public string? MessageId { get; init; }

    public string? To { get; init; }

    public SmsDeliveryStatus Status { get; init; }

    public DateTimeOffset Timestamp { get; init; }

    public IDictionary<string, string> Raw { get; init; } = new Dictionary<string, string>();
}
