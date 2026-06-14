namespace SmsBridge.Options;

public sealed class PlivoOptions
{
    public const string SectionKey = "Plivo";

    public required string AuthId { get; init; }

    public required string AuthToken { get; init; }

    public required string From { get; init; }
}
