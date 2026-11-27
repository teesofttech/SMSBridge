namespace SmsBridge.Options;

/// <summary>Configuration for LINK Mobility Common Platform outbound SMS.</summary>
public sealed class LinkMobilityOptions
{
    internal const string DefaultBaseUrl = "https://wsx.sp247.net";

    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string PlatformId { get; init; }
    public required string PlatformPartnerId { get; init; }
    public required string From { get; init; }

    /// <summary>The HTTPS origin assigned by LINK Mobility, without the SMS path.</summary>
    public string BaseUrl { get; init; } = DefaultBaseUrl;
}
