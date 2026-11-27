namespace SmsBridge.Options;

/// <summary>Configuration for Textmagic REST API V2 outbound SMS.</summary>
public sealed class TextmagicOptions
{
    public required string Username { get; init; }
    public required string ApiKey { get; init; }
    public required string From { get; init; }
}
