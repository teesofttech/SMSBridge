using System.Security.Cryptography;
using System.Text;
using NSec.Cryptography;
using SmsBridge.Abstractions;

namespace SmsBridge.Webhooks;

/// <summary>Authenticates Telnyx JSON callbacks using Ed25519.</summary>
public sealed class TelnyxWebhookVerifier : SmsWebhookVerifier
{
    private const string SignatureHeader = "telnyx-signature-ed25519";
    private const string TimestampHeader = "telnyx-timestamp";
    private static readonly TimeSpan DefaultReplayWindow = TimeSpan.FromMinutes(5);
    private readonly PublicKey _publicKey;
    private readonly TimeSpan _replayWindow;
    private readonly TimeProvider _timeProvider;

    public TelnyxWebhookVerifier(
        string publicKey,
        TimeSpan? replayWindow = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicKey);

        byte[] publicKeyBytes;
        try
        {
            publicKeyBytes = Convert.FromBase64String(publicKey);
            _publicKey = PublicKey.Import(
                SignatureAlgorithm.Ed25519,
                publicKeyBytes,
                KeyBlobFormat.RawPublicKey);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("The Telnyx public key must be valid Base64.", nameof(publicKey), exception);
        }
        catch (Exception exception) when (exception is ArgumentException or CryptographicException)
        {
            throw new ArgumentException("The Telnyx public key is invalid.", nameof(publicKey), exception);
        }

        _replayWindow = WebhookSignatureHelpers.ValidateReplayWindow(replayWindow ?? DefaultReplayWindow);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public override SmsProviderType Provider => SmsProviderType.Telnyx;

    public override void Verify(SmsWebhookRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.TryGetHeader(SignatureHeader, out var suppliedSignature))
            throw WebhookSignatureHelpers.MissingHeader(SignatureHeader);
        if (!request.TryGetHeader(TimestampHeader, out var timestamp))
            throw WebhookSignatureHelpers.MissingHeader(TimestampHeader);

        WebhookSignatureHelpers.RequireFreshUnixTimestamp(timestamp, _replayWindow, _timeProvider);

        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(suppliedSignature);
        }
        catch (FormatException)
        {
            throw WebhookSignatureHelpers.InvalidSignature();
        }

        if (signature.Length != SignatureAlgorithm.Ed25519.SignatureSize)
            throw WebhookSignatureHelpers.InvalidSignature();

        var prefix = Encoding.UTF8.GetBytes(timestamp + "|");
        var signedValue = new byte[prefix.Length + request.Body.Length];
        prefix.CopyTo(signedValue, 0);
        request.Body.Span.CopyTo(signedValue.AsSpan(prefix.Length));

        if (!SignatureAlgorithm.Ed25519.Verify(_publicKey, signedValue, signature))
            throw WebhookSignatureHelpers.InvalidSignature();
    }

    protected override SmsWebhookEvent ParseVerified(
        SmsWebhookRequest request,
        ISmsWebhookParser parser) =>
        parser.ParseJson(Encoding.UTF8.GetString(request.Body.Span));
}
