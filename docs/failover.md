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

- Failover only triggers on transient failures (5xx, network errors, throttling).
- Failover does not trigger on permanent failures (invalid number, authentication error).
- Failover does not trigger when the message has an explicit `Provider` override.
- SMSBridge never silently sends duplicate messages.

## Result

`SmsSendResult.Provider` always contains the name of the provider that actually sent the message.
