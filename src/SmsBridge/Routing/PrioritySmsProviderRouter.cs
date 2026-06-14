using SmsBridge.Abstractions;

namespace SmsBridge.Routing;

/// <summary>
/// Planned — routes to providers in priority order. Not implemented in MVP.
/// </summary>
internal sealed class PrioritySmsProviderRouter : ISmsProviderRouter
{
    public ISmsProvider Resolve(SmsRoutingContext context) =>
        throw new SmsBridgeException("PrioritySmsProviderRouter is planned but not implemented in this version.");
}
