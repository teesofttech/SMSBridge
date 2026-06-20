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

When the primary provider returns a transient failure (5xx, throttling, network error), SMSBridge automatically retries via the failover provider. Application code is unchanged.

---

## Supported Providers

| Provider | Status |
|---|---|
| Twilio | Supported |
| Vonage | Supported |
| Sinch | Supported |
| Plivo | Supported |
| Telnyx | Supported |
| MessageBird | Planned |
| AWS SNS | Planned |
| Infobip | Planned |
| Termii | Planned |
| Unifonic | Planned |

---

## Roadmap

- MessageBird provider
- AWS SNS provider
- Infobip provider
- Termii provider
- Unifonic provider
- OpenTelemetry metrics
- Delivery receipt normalisation
- Webhook signature verification
- Database outbox pattern
- Advanced retry policies
- Background processing

---

## Target Frameworks

- .NET 8
- .NET 9

---

## Contributing

Contributions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for guidance.

---

## Licence

MIT. See [LICENSE](LICENSE).
