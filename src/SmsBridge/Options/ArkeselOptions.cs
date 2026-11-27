namespace SmsBridge.Options;

/// <summary>Configuration for the Arkesel SMS V2 API.</summary>
public sealed class ArkeselOptions
{
    public required string ApiKey { get; init; }
    public required string Sender { get; init; }
    public string? UseCase { get; init; }
}
