namespace SmsBridge.Options;

/// <summary>Configuration for Esendex Message Dispatcher outbound SMS.</summary>
public sealed class EsendexOptions
{
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string AccountReference { get; init; }
    public string? From { get; init; }
}
