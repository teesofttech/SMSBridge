using SmsBridge.Abstractions;
using SmsBridge.DependencyInjection;
using SmsBridge.Options;

var builder = WebApplication.CreateBuilder(args);

// Register SMSBridge with Twilio as the provider.
// To use a different provider, only the configuration changes — application code stays the same.
builder.Services.AddSmsBridge(opts =>
    {
        opts.DefaultProvider = "twilio";
        opts.Providers["twilio"] = new SmsProviderOptions { Type = SmsProviderType.Twilio };
    })
    .UseTwilio("twilio", o =>
    {
        o.AccountSid = builder.Configuration["SmsBridge:Providers:twilio:AccountSid"]!;
        o.AuthToken  = builder.Configuration["SmsBridge:Providers:twilio:AuthToken"]!;
        o.From       = builder.Configuration["SmsBridge:Providers:twilio:From"]!;
    });

builder.Services.AddScoped<NotificationService>();

var app = builder.Build();

app.MapPost("/send-otp", async (SendOtpRequest req, NotificationService svc) =>
{
    var result = await svc.SendOtpAsync(req.PhoneNumber, req.Code);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.Run();

// ─── Service ───────────────────────────────────────────────────────────────────
// Application code depends only on ISmsClient — it does not know which provider is used.

public sealed class NotificationService(ISmsClient smsClient)
{
    public async Task<SmsSendResult> SendOtpAsync(
        string phoneNumber,
        string code,
        CancellationToken ct = default) =>
        await smsClient.SendAsync(new SmsMessage
        {
            To   = phoneNumber,
            Body = $"Your verification code is {code}"
        }, ct);
}

public sealed record SendOtpRequest(string PhoneNumber, string Code);
