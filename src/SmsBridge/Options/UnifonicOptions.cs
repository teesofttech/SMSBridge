namespace SmsBridge.Options;

public sealed class UnifonicOptions
{
    public const string SectionKey = "Unifonic";

    public required string AppSid { get; init; }

    public required string From { get; init; }
}
