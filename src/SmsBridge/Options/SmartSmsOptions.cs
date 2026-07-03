namespace SmsBridge.Options;

public sealed class SmartSmsOptions
{
    public const string SectionKey = "SmartSms";

    public required string Token { get; init; }

    public required string From { get; init; }
}
