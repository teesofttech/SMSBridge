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

## Sinch

```csharp
.UseSinch("sinch", o =>
{
    o.ServicePlanId = "xxxxxxxx";
    o.ApiToken      = "xxxxxxxx";
    o.From          = "+15551234567";
})
```

Uses the Sinch SMS REST API directly. No official Sinch SDK dependency.

## Plivo

```csharp
.UsePlivo("plivo", o =>
{
    o.AuthId    = "MAXXXXXXXX";
    o.AuthToken = "xxxxxxxx";
    o.From      = "+15551234567";
})
```

Uses the Plivo SMS REST API directly. No official Plivo SDK dependency.

## Telnyx

```csharp
.UseTelnyx("telnyx", o =>
{
    o.ApiKey = "KEYxxxxxxxx";
    o.From   = "+15551234567";
})
```

Uses the Telnyx messaging REST API directly. No official Telnyx SDK dependency.

## Planned Providers

- MessageBird
- AWS SNS
- Infobip
- Termii
- Unifonic
