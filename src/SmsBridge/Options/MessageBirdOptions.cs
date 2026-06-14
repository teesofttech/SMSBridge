namespace SmsBridge.Options;

/// <summary>MessageBird provider options — planned, not yet implemented.</summary>
public sealed class MessageBirdOptions
{
    public const string SectionKey = "MessageBird";

    public required string AccessKey { get; init; }

    public required string From { get; init; }
}
