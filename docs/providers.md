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

## Termii

```csharp
.UseTermii("termii", o =>
{
    o.ApiKey  = "your-api-key";
    o.From    = "MyApp";
    o.Channel = "generic"; // optional, defaults to generic
    o.BaseUrl = "https://api.ng.termii.com"; // optional regional/account base URL
})
```

Uses Termii's Messaging API with JSON request bodies. SMSBridge submits the
documented `api_key`, `to`, `from`, `sms`, `type`, and `channel` fields.
`type` defaults to `plain`; `channel` defaults to `generic` and can be
overridden per message through metadata.

## SmartSMSSolutions

```csharp
.UseSmartSms("smart-sms", o =>
{
    o.Token = "your-token";
    o.From  = "SmsBridge";
})
```

Uses SmartSMSSolutions' API-x SMS endpoint with POST form data. SMSBridge
submits the documented `token`, `sender`, `to`, `message`, `type`, `routing`,
and `ref_id` fields. `type` defaults to `0`, `routing` defaults to `3`, and
`ref_id`, `simserver_token`, `dlr_timeout`, and `schedule` can be supplied
through message metadata.

## Africa's Talking

```csharp
.UseAfricasTalking("africas-talking", o =>
{
    o.Username = "your-username";
    o.ApiKey = "your-api-key";
    o.From = "SmsBridge"; // optional
});
```

Uses Africa's Talking's legacy bulk SMS endpoint with POST form data.
SMSBridge submits the documented `username`, `to`, `message`, and optional
`from` fields, authenticates with the `apiKey` request header, and supports
`bulkSMSMode`, `enqueue`, `keyword`, `linkId`, and `retryDurationInHours`
through message metadata.
