# SMSBridge

**Install one SDK. Send SMS through multiple providers. Switch anytime.**

SMSBridge is a provider-agnostic .NET SDK for sending SMS messages through multiple providers using one clean API.

SMSBridge is not a messaging platform. It is a developer SDK that gives .NET applications one clean interface for sending SMS through multiple providers.

---

## Why SMSBridge?

Every SMS provider has a different SDK, a different API shape, and different error codes.

If you use Twilio today and need to switch to Vonage tomorrow, you rewrite your SMS code.

SMSBridge solves this. You install one package. You write to one interface. You switch providers by changing one configuration value.

---

## Installation

```bash
dotnet add package SmsBridge
```

---

## Quick Start

### 1. Register SMSBridge

```csharp
builder.Services.AddSmsBridge(opts =>
    {
        opts.DefaultProvider = "twilio";
        opts.Providers["twilio"] = new SmsProviderOptions { Type = SmsProviderType.Twilio };
    })
    .UseTwilio("twilio", o =>
    {
        o.AccountSid = builder.Configuration["SmsBridge:Providers:twilio:AccountSid"];
        o.AuthToken  = builder.Configuration["SmsBridge:Providers:twilio:AuthToken"];
        o.From       = builder.Configuration["SmsBridge:Providers:twilio:From"];
        o.StatusCallbackUrl = "https://example.com/webhooks/twilio"; // optional
    });
```

Or bind entirely from configuration:

```csharp
builder.Services.AddSmsBridge(
    builder.Configuration.GetSection("SmsBridge"));
```

### 2. Inject and send

```csharp
public sealed class NotificationService(ISmsClient smsClient)
{
    public async Task SendOtpAsync(string phoneNumber, string code, CancellationToken ct = default) =>
        await smsClient.SendAsync(new SmsMessage
        {
            To   = phoneNumber,
            Body = $"Your verification code is {code}"
        }, ct);
}
```

Your application code depends only on `ISmsClient`. It never imports a provider namespace.

---

## Configuration-Based Provider Switching

Configure both providers, then switch by changing one value:

```json
{
  "SmsBridge": {
    "DefaultProvider": "twilio",
    "Providers": {
      "twilio": {
        "Type": "Twilio",
        "AccountSid": "...",
        "AuthToken": "...",
        "From": "+447700900000"
      },
      "vonage": {
        "Type": "Vonage",
        "ApiKey": "...",
        "ApiSecret": "...",
        "From": "MyApp"
      }
    }
  }
}
```

To switch from Twilio to Vonage, change:

```json
"DefaultProvider": "vonage"
```

No application code changes.

---

## Per-Message Provider Override

```csharp
await smsClient.SendAsync(new SmsMessage
{
    To       = "+447700900000",
    Body     = "Hello",
    Provider = "vonage"   // override for this message only
});
```

---

## Failover

```csharp
builder.Services.AddSmsBridge(opts =>
    {
        opts.DefaultProvider  = "twilio";
        opts.EnableFailover   = true;
        opts.FailoverProvider = "vonage";
        opts.Providers["twilio"] = new SmsProviderOptions { Type = SmsProviderType.Twilio };
        opts.Providers["vonage"] = new SmsProviderOptions { Type = SmsProviderType.Vonage };
    })
    .UseTwilio("twilio", o => { ... })
    .UseVonage("vonage", o => { ... });
```

SMSBridge fails over only when the primary failure is transient and known not to have
accepted the message, such as rate limiting or an explicit provider rejection. It does
not automatically fail over after a connection failure or `5xx` response because the
primary may already have accepted the message and retrying through another provider
could deliver a duplicate.

See [docs/failover.md](docs/failover.md) for the complete safety rules.

---

## Delivery Status Callbacks

Callback URLs can be configured for providers that support per-message delivery reports:

```csharp
.UseTwilio("twilio", o =>
{
    // credentials omitted
    o.StatusCallbackUrl = "https://example.com/webhooks/twilio";
})
.UsePlivo("plivo", o =>
{
    // credentials omitted
    o.CallbackUrl = "https://example.com/webhooks/plivo";
})
.UseSinch("sinch", o =>
{
    // credentials omitted
    o.BaseUrl = "https://eu.sms.api.sinch.com"; // defaults to US
    o.CallbackUrl = "https://example.com/webhooks/sinch";
});
```

Twilio, Vonage, and Plivo callbacks use form payloads. Sinch and Telnyx callbacks
use JSON payloads. Resolve the provider parser with `SmsWebhookParserResolver`, then
call `Parse(...)` or `ParseJson(...)` as appropriate.

---

## Supported Providers

| Provider | Status |
|---|---|
| Twilio | Supported |
| Vonage | Supported |
| Sinch | Supported |
| Plivo | Supported |
| Telnyx | Supported |
| MessageBird | Supported |
| AWS SNS | Supported |
| Infobip | Supported |
| SmartSMSSolutions | Supported |
| Termii | Supported |
| Unifonic | Supported |
| Africa's Talking | Supported |

---

## Roadmap

- OpenTelemetry metrics
- Webhook signature verification
- Database outbox pattern
- Advanced retry policies
- Background processing

---

## Target Frameworks

- .NET 8
- .NET 9
- .NET 10

---

## Contributing

Contributions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for guidance.

---

## Licence

MIT. See [LICENSE](LICENSE).
