namespace SmsBridge.Options;

/// <summary>AWS SNS provider options.</summary>
public sealed class AwsSnsOptions
{
    public const string SectionKey = "AwsSns";

    public required string AccessKeyId { get; init; }

    public required string SecretAccessKey { get; init; }

    public required string Region { get; init; }

    public string? SenderId { get; init; }
}
