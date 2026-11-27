---
title: Providers
layout: default
---

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
Telnyx supports a per-message `webhook_url`, but SMSBridge does not send one, so
configure delivery webhooks on the Telnyx Messaging Profile.

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

## AWS SNS

```csharp
.UseAwsSns("aws-sns", o =>
{
    o.AccessKeyId     = "AKIAxxxxxxxx";
    o.SecretAccessKey = "xxxxxxxx";
    o.Region          = "us-east-1";
    o.SenderId        = "MyApp"; // optional
})
```

Calls the Amazon SNS `Publish` action at `https://sns.{Region}.amazonaws.com/` with
POST form data signed with AWS Signature Version 4. No AWS SDK dependency.
`SenderId` is sent as the `AWS.SNS.SMS.SenderID` message attribute and can be
overridden per message with `SmsMessage.From`.

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

## Unifonic

```csharp
.UseUnifonic("unifonic", o =>
{
    o.AppSid = "your-app-sid";
    o.From   = "MyApp";
})
```

Uses Unifonic's `https://el.cloud.unifonic.com/rest/SMS/messages` endpoint with
POST and Basic authentication using the `AppSid`. SMSBridge sends `AppSid`,
`SenderID`, `Recipient`, `Body`, and `responseType` as query-string parameters
and strips a leading `+` or `00` from the recipient. `CorrelationID`,
`baseEncode`, `statusCallback`, and `async` can be supplied through message metadata.

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

## Arkesel (SMS V2)

```csharp
.UseArkesel("arkesel", o =>
{
    o.ApiKey = "your-api-key";
    o.Sender = "MyApp";
    o.UseCase = "transactional"; // optional, for Nigerian traffic
})
```

SMSBridge sends outbound messages through Arkesel's SMS V2 endpoint at
`https://sms.arkesel.com/api/v2/sms/send`. It puts the API key in the `api-key`
header and sends the sender, message and recipient in the JSON body.

Configure `Sender` with an approved sender ID of up to 11 characters.
`SmsMessage.From` can override it per message. For Nigerian traffic, set
`UseCase` to `transactional` or `promotional` as appropriate.
See the [Arkesel API documentation](https://developers.arkesel.com/) for details.

## seven.io

```csharp
.UseSevenIo("seven", o =>
{
    o.ApiKey = configuration["SevenIo:ApiKey"];
    o.From = "MyApp";
})
```

For configuration-based registration, set `Type` to `SevenIo` and supply
`ApiKey` and `From`. A message can override the configured sender through
`SmsMessage.From`.

SMSBridge sends a form-encoded POST to the SMS endpoint and authenticates
with the `X-Api-Key` header. Response code `100` means the gateway accepted the
message for sending. This plain-text response provides no message ID.
See the [seven.io SMS API](https://docs.seven.io/en/rest-api/endpoints/sms).

## Textmagic

```csharp
.UseTextmagic("textmagic", o =>
{
    o.Username = configuration["Textmagic:Username"];
    o.ApiKey = configuration["Textmagic:ApiKey"];
    o.From = "MyApp";
})
```

For configuration-based registration, set `Type` to `Textmagic` and supply
`Username`, `ApiKey` and `From`. Use a Textmagic REST API V2 key and a sender
ID or number available to your account. A message can override the configured
sender through `SmsMessage.From`.

SMSBridge sends a JSON POST to the V2 messages endpoint and authenticates with
the `X-TM-Username` and `X-TM-Key` headers. A single-message response supplies
the message ID in `SmsSendResult.ProviderMessageId`.
See the [Textmagic API reference](https://docs.textmagic.com/) for details.
