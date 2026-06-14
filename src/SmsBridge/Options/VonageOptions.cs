namespace SmsBridge.Options;

public sealed class VonageOptions
{
    public const string SectionKey = "Vonage";

    public required string ApiKey { get; init; }

    public required string ApiSecret { get; init; }

    public required string From { get; init; }
}
