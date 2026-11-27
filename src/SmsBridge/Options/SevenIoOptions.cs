namespace SmsBridge.Options;

/// <summary>Configuration for seven.io outbound SMS.</summary>
public sealed class SevenIoOptions
{
    public required string ApiKey { get; init; }
    public required string From { get; init; }
}
