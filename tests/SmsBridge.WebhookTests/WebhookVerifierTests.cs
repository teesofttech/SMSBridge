using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using NSec.Cryptography;
using SmsBridge.Abstractions;
using SmsBridge.Webhooks;

namespace SmsBridge.WebhookTests;

public sealed class WebhookVerifierTests
{
    private static readonly DateTimeOffset CurrentTime =
        DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);

    [Fact]
    public void Twilio_VerifyAndParse_WithValidSignature_ReturnsEvent()
    {
        const string url = "https://example.com/webhooks/twilio";
        const string token = "twilio-token";
        var parameters = TwilioParameters();
        var request = FormRequest(
            url,
            parameters,
            ("X-Twilio-Signature", TwilioSignature(url, parameters, token)));

        var result = new TwilioWebhookVerifier(token)
            .VerifyAndParse(request, new TwilioWebhookParser());

        result.Provider.Should().Be(SmsProviderType.Twilio);
        result.MessageId.Should().Be("SM123");
    }

    [Fact]
    public void Twilio_VerifyAndParse_WithTamperedParameters_RejectsBeforeParsing()
    {
        const string url = "https://example.com/webhooks/twilio";
        const string token = "twilio-token";
        var original = TwilioParameters();
        var tampered = TwilioParameters();
        tampered["MessageSid"] = "SM-tampered";
        var request = FormRequest(
            url,
            tampered,
            ("X-Twilio-Signature", TwilioSignature(url, original, token)));

        var act = () => new TwilioWebhookVerifier(token)
            .VerifyAndParse(request, new ThrowingParser(SmsProviderType.Twilio));

        act.Should().Throw<SmsWebhookValidationException>()
            .WithMessage("*signature is invalid*");
    }

    [Fact]
    public void Twilio_Verify_WithMissingSignature_RejectsRequest()
    {
        var request = FormRequest("https://example.com/webhooks/twilio", TwilioParameters());

        var act = () => new TwilioWebhookVerifier("twilio-token").Verify(request);

        act.Should().Throw<SmsWebhookValidationException>()
            .WithMessage("*X-Twilio-Signature*missing*");
    }

    [Fact]
    public void Plivo_VerifyAndParse_WithValidSignature_ReturnsEvent()
    {
        const string url = "https://example.com/webhooks/plivo";
        const string token = "plivo-token";
        const string nonce = "123456";
        var signature = Base64HmacSha256(token, url + nonce);
        var request = FormRequest(
            url,
            PlivoParameters(),
            ("X-Plivo-Signature-V2", signature),
            ("X-Plivo-Signature-V2-Nonce", nonce));

        var result = new PlivoWebhookVerifier(token)
            .VerifyAndParse(request, new PlivoWebhookParser());

        result.Provider.Should().Be(SmsProviderType.Plivo);
        result.MessageId.Should().Be("plivo-123");
    }

    [Fact]
    public void Plivo_Verify_WithTamperedUrl_RejectsRequest()
    {
        const string originalUrl = "https://example.com/webhooks/plivo";
        const string token = "plivo-token";
        const string nonce = "123456";
        var request = FormRequest(
            originalUrl + "?tampered=true",
            PlivoParameters(),
            ("X-Plivo-Signature-V2", Base64HmacSha256(token, originalUrl + nonce)),
            ("X-Plivo-Signature-V2-Nonce", nonce));

        var act = () => new PlivoWebhookVerifier(token).Verify(request);

        act.Should().Throw<SmsWebhookValidationException>()
            .WithMessage("*signature is invalid*");
    }

    [Fact]
    public void Vonage_VerifyAndParse_WithValidSignature_ReturnsEvent()
    {
        const string secret = "vonage-secret";
        var parameters = VonageParameters(CurrentTime.ToUnixTimeSeconds());
        parameters["sig"] = VonageSignature(parameters, secret);
        var request = FormRequest("https://example.com/webhooks/vonage", parameters);
        var verifier = new VonageWebhookVerifier(
            secret,
            timeProvider: new FixedTimeProvider(CurrentTime));

        var result = verifier.VerifyAndParse(request, new VonageWebhookParser());

        result.Provider.Should().Be(SmsProviderType.Vonage);
        result.MessageId.Should().Be("vonage-123");
    }

    [Fact]
    public void Vonage_Verify_WithTamperedParameters_RejectsRequest()
    {
        const string secret = "vonage-secret";
        var parameters = VonageParameters(CurrentTime.ToUnixTimeSeconds());
        parameters["sig"] = VonageSignature(parameters, secret);
        parameters["status"] = "FAILED";
        var verifier = new VonageWebhookVerifier(
            secret,
            timeProvider: new FixedTimeProvider(CurrentTime));

        var act = () => verifier.Verify(
            FormRequest("https://example.com/webhooks/vonage", parameters));

        act.Should().Throw<SmsWebhookValidationException>()
            .WithMessage("*signature is invalid*");
    }

    [Fact]
    public void Vonage_Verify_WithStaleTimestamp_RejectsReplay()
    {
        const string secret = "vonage-secret";
        var parameters = VonageParameters(CurrentTime.AddMinutes(-6).ToUnixTimeSeconds());
        parameters["sig"] = VonageSignature(parameters, secret);
        var verifier = new VonageWebhookVerifier(
            secret,
            replayWindow: TimeSpan.FromMinutes(5),
            timeProvider: new FixedTimeProvider(CurrentTime));

        var act = () => verifier.Verify(
            FormRequest("https://example.com/webhooks/vonage", parameters));

        act.Should().Throw<SmsWebhookValidationException>()
            .WithMessage("*outside the allowed replay window*");
    }

    [Fact]
    public void Sinch_VerifyAndParse_WithValidSignature_ReturnsEvent()
    {
        const string secret = "sinch-secret";
        const string nonce = "nonce-123";
        const string json = "{\"batch_id\":\"batch-123\",\"recipient\":\"+447700900123\",\"status\":\"Delivered\"}";
        var timestamp = CurrentTime.ToUnixTimeSeconds().ToString();
        var request = JsonRequest(
            json,
            ("X-Sinch-Webhook-Signature", SinchSignature(json, nonce, timestamp, secret)),
            ("X-Sinch-Webhook-Signature-Nonce", nonce),
            ("X-Sinch-Webhook-Signature-Timestamp", timestamp));
        var verifier = new SinchWebhookVerifier(
            secret,
            timeProvider: new FixedTimeProvider(CurrentTime));

        var result = verifier.VerifyAndParse(request, new SinchWebhookParser());

        result.Provider.Should().Be(SmsProviderType.Sinch);
        result.MessageId.Should().Be("batch-123");
    }

    [Fact]
    public void Sinch_Verify_WithTamperedBody_RejectsRequest()
    {
        const string secret = "sinch-secret";
        const string nonce = "nonce-123";
        const string originalJson = "{\"status\":\"Delivered\"}";
        const string tamperedJson = "{\"status\":\"Failed\"}";
        var timestamp = CurrentTime.ToUnixTimeSeconds().ToString();
        var request = JsonRequest(
            tamperedJson,
            ("X-Sinch-Webhook-Signature", SinchSignature(originalJson, nonce, timestamp, secret)),
            ("X-Sinch-Webhook-Signature-Nonce", nonce),
            ("X-Sinch-Webhook-Signature-Timestamp", timestamp));
        var verifier = new SinchWebhookVerifier(
            secret,
            timeProvider: new FixedTimeProvider(CurrentTime));

        var act = () => verifier.Verify(request);

        act.Should().Throw<SmsWebhookValidationException>()
            .WithMessage("*signature is invalid*");
    }

    [Fact]
    public void Sinch_Verify_WithFutureTimestamp_RejectsReplay()
    {
        const string secret = "sinch-secret";
        const string nonce = "nonce-123";
        const string json = "{}";
        var timestamp = CurrentTime.AddMinutes(6).ToUnixTimeSeconds().ToString();
        var request = JsonRequest(
            json,
            ("X-Sinch-Webhook-Signature", SinchSignature(json, nonce, timestamp, secret)),
            ("X-Sinch-Webhook-Signature-Nonce", nonce),
            ("X-Sinch-Webhook-Signature-Timestamp", timestamp));
        var verifier = new SinchWebhookVerifier(
            secret,
            timeProvider: new FixedTimeProvider(CurrentTime));

        var act = () => verifier.Verify(request);

        act.Should().Throw<SmsWebhookValidationException>()
            .WithMessage("*outside the allowed replay window*");
    }

    [Fact]
    public void Telnyx_VerifyAndParse_WithValidSignature_ReturnsEvent()
    {
        using var key = Key.Create(SignatureAlgorithm.Ed25519);
        const string json = "{\"data\":{\"occurred_at\":\"2023-11-14T22:13:20Z\",\"payload\":{\"id\":\"telnyx-123\",\"to\":[{\"phone_number\":\"+447700900123\",\"status\":\"delivered\"}]}}}";
        var timestamp = CurrentTime.ToUnixTimeSeconds().ToString();
        var request = TelnyxRequest(json, timestamp, key);
        var verifier = new TelnyxWebhookVerifier(
            Convert.ToBase64String(key.PublicKey.Export(KeyBlobFormat.RawPublicKey)),
            timeProvider: new FixedTimeProvider(CurrentTime));

        var result = verifier.VerifyAndParse(request, new TelnyxWebhookParser());

        result.Provider.Should().Be(SmsProviderType.Telnyx);
        result.MessageId.Should().Be("telnyx-123");
    }

    [Fact]
    public void Telnyx_Verify_WithTamperedBody_RejectsRequest()
    {
        using var key = Key.Create(SignatureAlgorithm.Ed25519);
        const string originalJson = "{\"status\":\"delivered\"}";
        const string tamperedJson = "{\"status\":\"failed\"}";
        var timestamp = CurrentTime.ToUnixTimeSeconds().ToString();
        var signature = TelnyxSignature(originalJson, timestamp, key);
        var request = JsonRequest(
            tamperedJson,
            ("telnyx-signature-ed25519", signature),
            ("telnyx-timestamp", timestamp));
        var verifier = new TelnyxWebhookVerifier(
            Convert.ToBase64String(key.PublicKey.Export(KeyBlobFormat.RawPublicKey)),
            timeProvider: new FixedTimeProvider(CurrentTime));

        var act = () => verifier.Verify(request);

        act.Should().Throw<SmsWebhookValidationException>()
            .WithMessage("*signature is invalid*");
    }

    [Fact]
    public void Telnyx_Verify_WithStaleTimestamp_RejectsReplay()
    {
        using var key = Key.Create(SignatureAlgorithm.Ed25519);
        const string json = "{}";
        var timestamp = CurrentTime.AddMinutes(-6).ToUnixTimeSeconds().ToString();
        var request = TelnyxRequest(json, timestamp, key);
        var verifier = new TelnyxWebhookVerifier(
            Convert.ToBase64String(key.PublicKey.Export(KeyBlobFormat.RawPublicKey)),
            replayWindow: TimeSpan.FromMinutes(5),
            timeProvider: new FixedTimeProvider(CurrentTime));

        var act = () => verifier.Verify(request);

        act.Should().Throw<SmsWebhookValidationException>()
            .WithMessage("*outside the allowed replay window*");
    }

    [Fact]
    public void VerifyAndParse_WithParserForAnotherProvider_RejectsRequest()
    {
        var verifier = new TwilioWebhookVerifier("twilio-token");
        var request = FormRequest("https://example.com/webhooks/twilio", TwilioParameters());

        var act = () => verifier.VerifyAndParse(request, new PlivoWebhookParser());

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Plivo parser cannot parse a Twilio webhook*");
    }

    private static Dictionary<string, string> TwilioParameters() => new()
    {
        ["MessageSid"] = "SM123",
        ["MessageStatus"] = "delivered",
        ["To"] = "+447700900123"
    };

    private static Dictionary<string, string> PlivoParameters() => new()
    {
        ["MessageUUID"] = "plivo-123",
        ["Status"] = "delivered",
        ["To"] = "+447700900123"
    };

    private static Dictionary<string, string> VonageParameters(long timestamp) => new()
    {
        ["messageId"] = "vonage-123",
        ["msisdn"] = "447700900123",
        ["status"] = "DELIVERED",
        ["timestamp"] = timestamp.ToString()
    };

    private static SmsWebhookRequest FormRequest(
        string url,
        IDictionary<string, string> parameters,
        params (string Name, string Value)[] headers) =>
        new(
            url,
            "POST",
            ReadOnlyMemory<byte>.Empty,
            headers.Select(header => new KeyValuePair<string, string>(header.Name, header.Value)),
            parameters);

    private static SmsWebhookRequest JsonRequest(
        string json,
        params (string Name, string Value)[] headers) =>
        new(
            "https://example.com/webhooks/json",
            "POST",
            Encoding.UTF8.GetBytes(json),
            headers.Select(header => new KeyValuePair<string, string>(header.Name, header.Value)));

    private static string TwilioSignature(
        string url,
        IDictionary<string, string> parameters,
        string token)
    {
        var signedValue = new StringBuilder(url);
        foreach (var parameter in parameters.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            signedValue.Append(parameter.Key).Append(parameter.Value);

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedValue.ToString())));
    }

    private static string Base64HmacSha256(string secret, string value)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private static string VonageSignature(IDictionary<string, string> parameters, string secret)
    {
        var signedValue = new StringBuilder();
        foreach (var parameter in parameters
                     .Where(pair => pair.Key != "sig")
                     .OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            signedValue
                .Append('&')
                .Append(parameter.Key)
                .Append('=')
                .Append(parameter.Value.Replace('&', '_').Replace('=', '_'));
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(
                hmac.ComputeHash(Encoding.UTF8.GetBytes(signedValue.ToString())))
            .ToLowerInvariant();
    }

    private static string SinchSignature(
        string json,
        string nonce,
        string timestamp,
        string secret) =>
        Base64HmacSha256(secret, $"{json}.{nonce}.{timestamp}");

    private static SmsWebhookRequest TelnyxRequest(string json, string timestamp, Key key) =>
        JsonRequest(
            json,
            ("telnyx-signature-ed25519", TelnyxSignature(json, timestamp, key)),
            ("telnyx-timestamp", timestamp));

    private static string TelnyxSignature(string json, string timestamp, Key key) =>
        Convert.ToBase64String(
            SignatureAlgorithm.Ed25519.Sign(
                key,
                Encoding.UTF8.GetBytes($"{timestamp}|{json}")));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class ThrowingParser(SmsProviderType provider) : ISmsWebhookParser
    {
        public SmsProviderType Provider => provider;

        public SmsWebhookEvent Parse(IDictionary<string, string> payload) =>
            throw new InvalidOperationException("The parser must not be called.");
    }
}
