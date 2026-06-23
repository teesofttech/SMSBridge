using SmsBridge.Abstractions;
using SmsBridge.DependencyInjection;
using SmsBridge.Options;

var builder = WebApplication.CreateBuilder(args);

// Twilio, Vonage, and MessageBird are configured.
// Switch providers by changing "DefaultProvider" in appsettings.json only.
// No application code changes required.
builder.Services.AddSmsBridge(opts =>
    {
        opts.DefaultProvider = builder.Configuration["SmsBridge:DefaultProvider"]!;
        opts.Providers["twilio"] = new SmsProviderOptions { Type = SmsProviderType.Twilio };
        opts.Providers["vonage"] = new SmsProviderOptions { Type = SmsProviderType.Vonage };
        opts.Providers["messagebird"] = new SmsProviderOptions { Type = SmsProviderType.MessageBird };
    })
    .UseTwilio("twilio", o =>
    {
        o.AccountSid = builder.Configuration["SmsBridge:Providers:twilio:AccountSid"]!;
        o.AuthToken  = builder.Configuration["SmsBridge:Providers:twilio:AuthToken"]!;
        o.From       = builder.Configuration["SmsBridge:Providers:twilio:From"]!;
    })
    .UseVonage("vonage", o =>
    {
        o.ApiKey    = builder.Configuration["SmsBridge:Providers:vonage:ApiKey"]!;
        o.ApiSecret = builder.Configuration["SmsBridge:Providers:vonage:ApiSecret"]!;
        o.From      = builder.Configuration["SmsBridge:Providers:vonage:From"]!;
    })
    .UseMessageBird("messagebird", o =>
    {
        o.AccessKey = builder.Configuration["SmsBridge:Providers:messagebird:AccessKey"]!;
        o.From      = builder.Configuration["SmsBridge:Providers:messagebird:From"]!;
    });

var app = builder.Build();

app.MapPost("/send", async (SendRequest req, ISmsClient smsClient) =>
{
    var result = await smsClient.SendAsync(new SmsMessage
    {
        To   = req.PhoneNumber,
        Body = req.Message
    });
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.Run();

public sealed record SendRequest(string PhoneNumber, string Message);
