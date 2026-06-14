# Providers

## Twilio

```csharp
.UseTwilio("twilio", o =>
{
    o.AccountSid = "ACxxxxxxxx";
    o.AuthToken  = "xxxxxxxx";
    o.From       = "+15551234567";
})
```

Uses the Twilio REST API directly. No official Twilio SDK dependency.

## Vonage

```csharp
.UseVonage("vonage", o =>
{
    o.ApiKey    = "xxxxxxxx";
    o.ApiSecret = "xxxxxxxx";
    o.From      = "MyApp";
})
```

Uses the Vonage SMS API v1 directly. No official Vonage SDK dependency.

## Planned Providers

- MessageBird
- AWS SNS
- Infobip
- Termii
- Unifonic
