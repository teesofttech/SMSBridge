using SmsBridge.Abstractions;

namespace SmsBridge.Routing;

internal sealed class SmsRoutingContext
{
    public required SmsMessage Message { get; init; }

    public string? ExplicitProvider { get; init; }
}
