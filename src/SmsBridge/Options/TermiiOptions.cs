namespace SmsBridge.Options;

public sealed class TermiiOptions
{
    public const string SectionKey = "Termii";

    public required string ApiKey { get; init; }

    public required string From { get; init; }

    public string Channel { get; init; } = "generic";

    public string BaseUrl { get; init; } = "https://api.ng.termii.com";
}
