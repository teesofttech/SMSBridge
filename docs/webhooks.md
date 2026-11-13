---
title: Webhooks
layout: default
---

# Webhooks

SMSBridge authenticates provider callbacks and parses their payloads into a normalised
`SmsWebhookEvent`. Always use `VerifyAndParse(...)` for an untrusted HTTP request. Calling a
parser directly does not authenticate the payload.

`SmsWebhookRequest` takes a snapshot of the exact public URL, HTTP method, raw body, headers,
and decoded form or query parameters. Do not parse, reserialise, trim, or otherwise change a
JSON body before constructing it. When an application is behind a proxy, pass the public URL
configured with the provider rather than a rewritten internal URL.

## ASP.NET Core form callback

Twilio, Vonage, and Plivo use flat form or query values. This example verifies a Twilio status
callback before parsing it:

```csharp
app.MapPost("/webhooks/twilio", async (
    HttpRequest request,
    SmsWebhookParserResolver resolver,
    IConfiguration configuration) =>
{
    var form = await request.ReadFormAsync();
    var parameters = form.ToDictionary(pair => pair.Key, pair => pair.Value.ToString());
    var headers = request.Headers.ToDictionary(pair => pair.Key, pair => pair.Value.ToString());

    var webhook = new SmsWebhookRequest(
        configuration["Twilio:WebhookUrl"]!, // Exact public URL configured in Twilio
        request.Method,
        ReadOnlyMemory<byte>.Empty,
        headers,
        parameters);

    var verifier = new TwilioWebhookVerifier(configuration["Twilio:AuthToken"]!);

    try
    {
        var parser = resolver.Resolve(SmsProviderType.Twilio);
        var evt = verifier.VerifyAndParse(webhook, parser);

        // Process only the authenticated event.
        return Results.Ok(evt);
    }
    catch (SmsWebhookValidationException)
    {
        return Results.Unauthorized();
    }
});
```

For a Vonage GET callback, take `parameters` from `request.Query` and use
`VonageWebhookVerifier`. For Plivo, use `PlivoWebhookVerifier` with the account or subaccount
auth token that generated `X-Plivo-Signature-V2`.

## ASP.NET Core JSON callback

Sinch and Telnyx signatures cover the unmodified request body. Capture the body as bytes and
let `VerifyAndParse(...)` decode it only after verification:

```csharp
app.MapPost("/webhooks/telnyx", async (
    HttpRequest request,
    SmsWebhookParserResolver resolver,
    IConfiguration configuration) =>
{
    await using var buffer = new MemoryStream();
    await request.Body.CopyToAsync(buffer);

    var headers = request.Headers.ToDictionary(pair => pair.Key, pair => pair.Value.ToString());
    var webhook = new SmsWebhookRequest(
        configuration["Telnyx:WebhookUrl"]!,
        request.Method,
        buffer.ToArray(),
        headers);

    var verifier = new TelnyxWebhookVerifier(configuration["Telnyx:PublicKey"]!);

    try
    {
        var parser = resolver.Resolve(SmsProviderType.Telnyx);
        var evt = verifier.VerifyAndParse(webhook, parser);
        return Results.Ok(evt);
    }
    catch (SmsWebhookValidationException)
    {
        return Results.Unauthorized();
    }
});
```

## Signature support

| Provider | Verifier | Authenticated data | Replay handling |
|---|---|---|---|
| Twilio | `TwilioWebhookVerifier` | Exact URL and all form parameters; HMAC-SHA1 | Deduplicate by message/event ID |
| Vonage | `VonageWebhookVerifier` | All parameters except `sig`; HMAC-SHA256 or HMAC-SHA512 | Unix `timestamp`; five-minute default window |
| Plivo | `PlivoWebhookVerifier` | Exact base URL and V2 nonce; HMAC-SHA256 | Persist V2 nonces or deduplicate event IDs |
| Sinch | `SinchWebhookVerifier` | Raw body, nonce, and timestamp; HMAC-SHA256 | Unix timestamp; five-minute default window |
| Telnyx | `TelnyxWebhookVerifier` | Timestamp and raw body; Ed25519 | Unix timestamp; five-minute default window |

The Vonage, Sinch, and Telnyx constructors accept a custom `replayWindow`. Their default is
five minutes and requests outside the window, including requests too far in the future, are
rejected. Timestamp windows limit replay exposure but do not guarantee single use; store seen
nonces or provider event IDs when an endpoint must reject every duplicate.

Signature byte comparisons use constant-time operations. Telnyx verification uses the public
key from the Telnyx portal, encoded as Base64. Do not use the private API credential.

### Provider configuration notes

- Vonage signed SMS callbacks are opt-in. Ask Vonage support to enable them, select SHA-256
  HMAC or SHA-512 HMAC in the dashboard, and pass the signature secret rather than the API
  secret.
- Sinch SMS callback authentication must be enabled for the account. Pass the callback signing
  secret, not the SMS API token.
- The Plivo verifier validates `X-Plivo-Signature-V2`. If you validate the main-account
  `X-Plivo-Signature-Ma-V2` header instead, use Plivo's own SDK until SMSBridge adds an explicit
  main-account mode.
- Use HTTPS for every callback endpoint and keep signing secrets outside source control.

Provider specifications:
[Twilio](https://www.twilio.com/docs/usage/webhooks/webhooks-security),
[Vonage](https://developer.vonage.com/en/getting-started/concepts/signing-messages),
[Plivo](https://docs.plivo.com/docs/messaging/concepts/signature-validation),
[Sinch](https://developers.sinch.com/docs/sdks/node/sdk-reference-docs), and
[Telnyx](https://developers.telnyx.com/docs/development/validate-webhooks).

## Supported webhook providers

| Provider | Parser | Signature verifier |
|---|---|---|
| Twilio | Supported | Supported |
| Vonage | Supported | Supported |
| Plivo | Supported | Supported |
| Sinch | Supported | Supported |
| Telnyx | Supported | Supported |
| MessageBird | Planned | Planned |

The Vonage parser accepts flat GET query or POST form fields. Set the SMS API **Webhook format**
to `GET` or `POST`; `POST-JSON` is not supported by the built-in parser.

## Planned

- Automatic delivery receipt persistence
