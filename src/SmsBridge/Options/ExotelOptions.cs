namespace SmsBridge.Options;

public enum ExotelRegion
{
    Singapore,
    Mumbai
}

/// <summary>Configuration for Exotel outbound SMS.</summary>
public sealed class ExotelOptions
{
    public required string AccountSid { get; init; }
    public required string ApiKey { get; init; }
    public required string AuthToken { get; init; }
    public required string From { get; init; }
    public ExotelRegion Region { get; init; } = ExotelRegion.Singapore;
    public string? DltEntityId { get; init; }
    public string? DltTemplateId { get; init; }
}
