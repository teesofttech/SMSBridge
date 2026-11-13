using System.Security.Cryptography;
using System.Text;
using SmsBridge.Abstractions;

namespace SmsBridge.Webhooks;

/// <summary>Authenticates signed Vonage query or form callbacks.</summary>
public sealed class VonageWebhookVerifier : SmsWebhookVerifier
{
    private static readonly TimeSpan DefaultReplayWindow = TimeSpan.FromMinutes(5);
    private readonly byte[] _signatureSecret;
    private readonly VonageWebhookSignatureAlgorithm _algorithm;
    private readonly TimeSpan _replayWindow;
    private readonly TimeProvider _timeProvider;

    public VonageWebhookVerifier(
        string signatureSecret,
        VonageWebhookSignatureAlgorithm algorithm = VonageWebhookSignatureAlgorithm.HmacSha256,
        TimeSpan? replayWindow = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signatureSecret);
        if (!Enum.IsDefined(algorithm))
            throw new ArgumentOutOfRangeException(nameof(algorithm));

        _signatureSecret = Encoding.UTF8.GetBytes(signatureSecret);
        _algorithm = algorithm;
        _replayWindow = WebhookSignatureHelpers.ValidateReplayWindow(replayWindow ?? DefaultReplayWindow);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public override SmsProviderType Provider => SmsProviderType.Vonage;

    public override void Verify(SmsWebhookRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parameters = request.GetRequiredParameters();
        if (!parameters.TryGetValue("sig", out var suppliedSignature))
            throw new SmsWebhookValidationException("The required webhook parameter 'sig' is missing.");
        if (!parameters.TryGetValue("timestamp", out var timestamp))
            throw new SmsWebhookValidationException("The required webhook parameter 'timestamp' is missing.");

        WebhookSignatureHelpers.RequireFreshUnixTimestamp(timestamp, _replayWindow, _timeProvider);

        var signedValue = new StringBuilder();
        foreach (var parameter in parameters
                     .Where(pair => !string.Equals(pair.Key, "sig", StringComparison.Ordinal))
                     .OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            signedValue
                .Append('&')
                .Append(parameter.Key)
                .Append('=')
                .Append(EscapeValue(parameter.Value));
        }

        using HMAC hmac = _algorithm switch
        {
            VonageWebhookSignatureAlgorithm.HmacSha256 => new HMACSHA256(_signatureSecret),
            VonageWebhookSignatureAlgorithm.HmacSha512 => new HMACSHA512(_signatureSecret),
            _ => throw new InvalidOperationException("Unsupported Vonage signature algorithm.")
        };
        var expectedSignature = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedValue.ToString()));
        WebhookSignatureHelpers.RequireValidHexSignature(suppliedSignature, expectedSignature);
    }

    protected override SmsWebhookEvent ParseVerified(
        SmsWebhookRequest request,
        ISmsWebhookParser parser) =>
        parser.Parse(request.GetRequiredParameters());

    private static string EscapeValue(string value) =>
        value.Replace('&', '_').Replace('=', '_');
}
