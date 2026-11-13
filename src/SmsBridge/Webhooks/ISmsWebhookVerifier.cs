using SmsBridge.Abstractions;

namespace SmsBridge.Webhooks;

/// <summary>Authenticates provider webhooks before their payload is parsed.</summary>
public interface ISmsWebhookVerifier
{
    SmsProviderType Provider { get; }

    /// <summary>Authenticates a request or throws <see cref="SmsWebhookValidationException"/>.</summary>
    void Verify(SmsWebhookRequest request);

    /// <summary>Authenticates and then parses a request into a normalised event.</summary>
    SmsWebhookEvent VerifyAndParse(SmsWebhookRequest request, ISmsWebhookParser parser);
}
