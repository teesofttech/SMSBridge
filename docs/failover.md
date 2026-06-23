# Failover

Enable failover so that transient provider failures are retried automatically via a backup provider.

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

## Failover Rules

- Failover only triggers when a failure is transient and known not to have accepted the message.
- Rate limiting and explicit pre-acceptance provider rejections may trigger failover.
- Connection failures and provider `5xx` responses do not trigger automatic failover because
  the primary provider may have accepted the message before the response was lost.
- Failover does not trigger on permanent failures (invalid number, authentication error).
- Failover does not trigger when the message has an explicit `Provider` override.
- This conservative policy prevents SMSBridge from silently creating a duplicate through
  automatic failover. Applications that manually retry ambiguous failures must provide their
  own idempotency or reconciliation strategy.

## Result

`SmsSendResult.Provider` always contains the name of the provider that actually sent the message.
