namespace SmsBridge.Options;

/// <summary>Configuration for the MessageBird SMS provider.</summary>
public sealed class MessageBirdOptions
{
    public const string SectionKey = "MessageBird";

    public required string AccessKey { get; init; }

    public required string From { get; init; }
}
