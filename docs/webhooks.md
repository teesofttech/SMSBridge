# Webhooks

SMSBridge can parse incoming provider webhook payloads into a normalised `SmsWebhookEvent`.

## Resolve a Parser

```csharp
app.MapPost("/webhooks/twilio", async (HttpRequest request, SmsWebhookParserResolver resolver) =>
{
    var payload = await request.ReadFormAsync();
    var dict = payload.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
    var parser = resolver.Resolve(SmsProviderType.Twilio);
    var evt = parser.Parse(dict);

    // evt.Status, evt.MessageId, evt.To are normalised across providers
    return Results.Ok(evt);
});
```

## Supported Webhook Providers

| Provider | Status |
|---|---|
| Twilio | Supported |
| Vonage | Supported |
| MessageBird | Planned |

## Planned

- Webhook signature verification
- Automatic delivery receipt persistence
