# Providers

## Twilio

```csharp
.UseTwilio("twilio", o =>
{
    o.AccountSid = "ACxxxxxxxx";
    o.AuthToken  = "xxxxxxxx";
    o.From       = "+15551234567";
    o.StatusCallbackUrl = "https://example.com/webhooks/twilio"; // optional
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
    o.BaseUrl       = "https://us.sms.api.sinch.com";
    o.CallbackUrl   = "https://example.com/webhooks/sinch"; // optional
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
    o.CallbackUrl = "https://example.com/webhooks/plivo"; // optional
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
Telnyx delivery webhooks are configured on the Messaging Profile rather than per message.

## MessageBird

```csharp
.UseMessageBird("messagebird", o =>
{
    o.AccessKey = "live_xxxxxxxx";
    o.From      = "MyApp";
})
```

Uses the MessageBird SMS API directly with `AccessKey` authentication. SMSBridge
submits URL-encoded message fields and requests automatic GSM/Unicode data coding.

## Infobip

```csharp
.UseInfobip("infobip", o =>
{
    o.ApiKey  = "your-api-key";
    o.BaseUrl = "https://xxxxx.api.infobip.com";
    o.From    = "MyApp";
})
```

Uses Infobip's current SMS API v3 endpoint with `App` API-key authentication.
The base URL is account-specific and must be an absolute HTTPS URL.

## Planned Providers

- AWS SNS
- Termii
- Unifonic
