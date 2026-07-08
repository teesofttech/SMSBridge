---
title: Webhooks
layout: default
---

# Webhooks

SMSBridge can parse incoming provider webhook payloads into a normalised `SmsWebhookEvent`.

Twilio, Vonage, and Plivo send flat form payloads and use `Parse(...)`. Sinch and
Telnyx send structured JSON payloads and use `ParseJson(...)`.

## Resolve a Parser

```csharp
app.MapPost("/webhooks/twilio", async (HttpRequest request, SmsWebhookParserResolver resolver) =>
{
    var payload = await request.ReadFormAsync();
    var dict = payload.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
    var parser = resolver.Resolve(SmsProviderType.Twilio);
    var evt = parser.Parse(dict);

    // evt.Status, evt.MessageId, evt.To and delivery error metadata are normalised
    return Results.Ok(evt);
});
```

For a JSON callback:

```csharp
app.MapPost("/webhooks/telnyx", async (
    HttpRequest request,
    SmsWebhookParserResolver resolver) =>
{
    using var reader = new StreamReader(request.Body);
    var json = await reader.ReadToEndAsync();
    var parser = resolver.Resolve(SmsProviderType.Telnyx);
    var evt = parser.ParseJson(json);

    return Results.Ok(evt);
});
```

## Supported Webhook Providers

| Provider | Status |
|---|---|
| Twilio | Supported |
| Vonage | Supported |
| Plivo | Supported |
| Sinch | Supported |
| Telnyx | Supported |
| MessageBird | Planned |

## Planned

- Webhook signature verification
- Automatic delivery receipt persistence
