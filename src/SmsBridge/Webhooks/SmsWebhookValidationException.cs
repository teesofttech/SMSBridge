using SmsBridge.Abstractions;

namespace SmsBridge.Webhooks;

/// <summary>Thrown when a webhook cannot be authenticated.</summary>
public sealed class SmsWebhookValidationException : SmsBridgeException
{
    public SmsWebhookValidationException(string message) : base(message) { }
}
