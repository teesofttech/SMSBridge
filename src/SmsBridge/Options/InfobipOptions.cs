namespace SmsBridge.Options;

/// <summary>Configuration for the Infobip SMS provider.</summary>
public sealed class InfobipOptions
{
    public const string SectionKey = "Infobip";

    public required string ApiKey { get; init; }

    /// <summary>Infobip account base URL, for example https://xxxxx.api.infobip.com.</summary>
    public required string BaseUrl { get; init; }

    public required string From { get; init; }
}
