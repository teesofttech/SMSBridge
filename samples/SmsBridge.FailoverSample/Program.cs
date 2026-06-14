using SmsBridge.Abstractions;
using SmsBridge.DependencyInjection;
using SmsBridge.Options;

var builder = WebApplication.CreateBuilder(args);

// Twilio is the primary provider. If it returns a transient failure,
// SMSBridge automatically retries via Vonage — with no application code changes.
builder.Services.AddSmsBridge(opts =>
    {
        opts.DefaultProvider  = "twilio";
        opts.EnableFailover   = true;
        opts.FailoverProvider = "vonage";
        opts.Providers["twilio"] = new SmsProviderOptions { Type = SmsProviderType.Twilio };
        opts.Providers["vonage"] = new SmsProviderOptions { Type = SmsProviderType.Vonage };
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
    });

var app = builder.Build();

app.MapPost("/send", async (SendRequest req, ISmsClient smsClient) =>
{
    // Application code is unchanged regardless of which provider handles the message.
    var result = await smsClient.SendAsync(new SmsMessage
    {
        To   = req.PhoneNumber,
        Body = req.Message
    });
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.Run();

public sealed record SendRequest(string PhoneNumber, string Message);
