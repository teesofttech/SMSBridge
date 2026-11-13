using System.Security.Cryptography;
using System.Text;
using SmsBridge.Abstractions;

namespace SmsBridge.Webhooks;

/// <summary>Authenticates Twilio form callbacks using <c>X-Twilio-Signature</c>.</summary>
public sealed class TwilioWebhookVerifier : SmsWebhookVerifier
{
    private const string SignatureHeader = "X-Twilio-Signature";
    private readonly byte[] _authToken;

    public TwilioWebhookVerifier(string authToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authToken);
        _authToken = Encoding.UTF8.GetBytes(authToken);
    }

    public override SmsProviderType Provider => SmsProviderType.Twilio;

    public override void Verify(SmsWebhookRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.TryGetHeader(SignatureHeader, out var suppliedSignature))
            throw WebhookSignatureHelpers.MissingHeader(SignatureHeader);

        var parameters = request.GetRequiredParameters();
        var signedValue = new StringBuilder(request.Url);
        foreach (var parameter in parameters.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            signedValue.Append(parameter.Key).Append(parameter.Value);

        using var hmac = new HMACSHA1(_authToken);
        var expectedSignature = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedValue.ToString()));
        WebhookSignatureHelpers.RequireValidBase64Signature(suppliedSignature, expectedSignature);
    }

    protected override SmsWebhookEvent ParseVerified(
        SmsWebhookRequest request,
        ISmsWebhookParser parser) =>
        parser.Parse(request.GetRequiredParameters());
}
