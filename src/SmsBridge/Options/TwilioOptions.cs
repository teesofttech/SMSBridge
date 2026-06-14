namespace SmsBridge.Options;

public sealed class TwilioOptions
{
    public const string SectionKey = "Twilio";

    public required string AccountSid { get; init; }

    public required string AuthToken { get; init; }

    public required string From { get; init; }
}
