namespace SmsBridge.Options;

public sealed class SinchOptions
{
    public const string SectionKey = "Sinch";

    public required string ServicePlanId { get; init; }

    public required string ApiToken { get; init; }

    public required string From { get; init; }
}
