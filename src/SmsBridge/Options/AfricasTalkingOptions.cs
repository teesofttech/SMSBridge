namespace SmsBridge.Options;

public sealed class AfricasTalkingOptions
{
    public const string SectionKey = "AfricasTalking";

    public required string Username { get; init; }

    public required string ApiKey { get; init; }

    public string? From { get; init; }
}
