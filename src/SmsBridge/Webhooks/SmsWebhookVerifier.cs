using SmsBridge.Abstractions;

namespace SmsBridge.Webhooks;

/// <summary>Base implementation that guarantees verification happens before parsing.</summary>
public abstract class SmsWebhookVerifier : ISmsWebhookVerifier
{
    public abstract SmsProviderType Provider { get; }

    public abstract void Verify(SmsWebhookRequest request);

    public SmsWebhookEvent VerifyAndParse(SmsWebhookRequest request, ISmsWebhookParser parser)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(parser);

        if (parser.Provider != Provider)
        {
            throw new ArgumentException(
                $"The {parser.Provider} parser cannot parse a {Provider} webhook.",
                nameof(parser));
        }

        Verify(request);
        return ParseVerified(request, parser);
    }

    protected abstract SmsWebhookEvent ParseVerified(
        SmsWebhookRequest request,
        ISmsWebhookParser parser);
}
