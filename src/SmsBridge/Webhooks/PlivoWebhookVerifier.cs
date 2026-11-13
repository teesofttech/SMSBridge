using System.Security.Cryptography;
using System.Text;
using SmsBridge.Abstractions;

namespace SmsBridge.Webhooks;

/// <summary>Authenticates Plivo messaging callbacks using its V2 signature headers.</summary>
public sealed class PlivoWebhookVerifier : SmsWebhookVerifier
{
    private const string SignatureHeader = "X-Plivo-Signature-V2";
    private const string NonceHeader = "X-Plivo-Signature-V2-Nonce";
    private readonly byte[] _authToken;

    public PlivoWebhookVerifier(string authToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authToken);
        _authToken = Encoding.UTF8.GetBytes(authToken);
    }

    public override SmsProviderType Provider => SmsProviderType.Plivo;

    public override void Verify(SmsWebhookRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.TryGetHeader(SignatureHeader, out var suppliedSignature))
            throw WebhookSignatureHelpers.MissingHeader(SignatureHeader);
        if (!request.TryGetHeader(NonceHeader, out var nonce))
            throw WebhookSignatureHelpers.MissingHeader(NonceHeader);

        using var hmac = new HMACSHA256(_authToken);
        var expectedSignature = hmac.ComputeHash(Encoding.UTF8.GetBytes(request.Url + nonce));
        WebhookSignatureHelpers.RequireValidBase64Signature(suppliedSignature, expectedSignature);
    }

    protected override SmsWebhookEvent ParseVerified(
        SmsWebhookRequest request,
        ISmsWebhookParser parser) =>
        parser.Parse(request.GetRequiredParameters());
}
