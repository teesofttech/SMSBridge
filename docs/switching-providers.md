# Switching Providers

Register both providers:

```csharp
builder.Services.AddSmsBridge(opts =>
    {
        opts.DefaultProvider = "twilio";   // change to "vonage" to switch
        opts.Providers["twilio"] = new SmsProviderOptions { Type = SmsProviderType.Twilio };
        opts.Providers["vonage"] = new SmsProviderOptions { Type = SmsProviderType.Vonage };
    })
    .UseTwilio("twilio", o => { ... })
    .UseVonage("vonage", o => { ... });
```

Switch by changing `DefaultProvider` only. No application code changes.

To override for a single message:

```csharp
await smsClient.SendAsync(new SmsMessage
{
    To       = "+447700900000",
    Body     = "Hello",
    Provider = "vonage"
});
```
