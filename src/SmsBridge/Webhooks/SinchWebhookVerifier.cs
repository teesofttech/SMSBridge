using System.Security.Cryptography;
using System.Text;
using SmsBridge.Abstractions;

namespace SmsBridge.Webhooks;

/// <summary>Authenticates Sinch JSON callbacks using its HMAC-SHA256 signature.</summary>
public sealed class SinchWebhookVerifier : SmsWebhookVerifier
{
    private const string SignatureHeader = "X-Sinch-Webhook-Signature";
    private const string NonceHeader = "X-Sinch-Webhook-Signature-Nonce";
    private const string TimestampHeader = "X-Sinch-Webhook-Signature-Timestamp";
    private static readonly TimeSpan DefaultReplayWindow = TimeSpan.FromMinutes(5);
    private readonly byte[] _signingSecret;
    private readonly TimeSpan _replayWindow;
    private readonly TimeProvider _timeProvider;

    public SinchWebhookVerifier(
        string signingSecret,
        TimeSpan? replayWindow = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signingSecret);
        _signingSecret = Encoding.UTF8.GetBytes(signingSecret);
        _replayWindow = WebhookSignatureHelpers.ValidateReplayWindow(replayWindow ?? DefaultReplayWindow);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public override SmsProviderType Provider => SmsProviderType.Sinch;

    public override void Verify(SmsWebhookRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.TryGetHeader(SignatureHeader, out var suppliedSignature))
            throw WebhookSignatureHelpers.MissingHeader(SignatureHeader);
        if (!request.TryGetHeader(NonceHeader, out var nonce))
            throw WebhookSignatureHelpers.MissingHeader(NonceHeader);
        if (!request.TryGetHeader(TimestampHeader, out var timestamp))
            throw WebhookSignatureHelpers.MissingHeader(TimestampHeader);

        WebhookSignatureHelpers.RequireFreshUnixTimestamp(timestamp, _replayWindow, _timeProvider);

        var suffix = Encoding.UTF8.GetBytes($".{nonce}.{timestamp}");
        var signedValue = new byte[request.Body.Length + suffix.Length];
        request.Body.Span.CopyTo(signedValue);
        suffix.CopyTo(signedValue.AsSpan(request.Body.Length));

        using var hmac = new HMACSHA256(_signingSecret);
        var expectedSignature = hmac.ComputeHash(signedValue);
        WebhookSignatureHelpers.RequireValidBase64Signature(suppliedSignature, expectedSignature);
    }

    protected override SmsWebhookEvent ParseVerified(
        SmsWebhookRequest request,
        ISmsWebhookParser parser) =>
        parser.ParseJson(Encoding.UTF8.GetString(request.Body.Span));
}
