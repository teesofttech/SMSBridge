namespace SmsBridge.Webhooks;

/// <summary>Secure HMAC algorithms supported for signed Vonage callbacks.</summary>
public enum VonageWebhookSignatureAlgorithm
{
    HmacSha256 = 0,
    HmacSha512 = 1
}
