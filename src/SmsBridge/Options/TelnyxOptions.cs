namespace SmsBridge.Options;

public sealed class TelnyxOptions
{
    public const string SectionKey = "Telnyx";

    public required string ApiKey { get; init; }

    public required string From { get; init; }
}
