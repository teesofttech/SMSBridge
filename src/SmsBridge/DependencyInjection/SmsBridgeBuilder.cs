using Microsoft.Extensions.DependencyInjection;

namespace SmsBridge.DependencyInjection;

/// <summary>
/// Builder returned from <c>AddSmsBridge</c>.
/// Use it to register providers via <c>UseTwilio</c>, <c>UseVonage</c>, etc.
/// </summary>
public sealed class SmsBridgeBuilder
{
    internal IServiceCollection Services { get; }

    internal SmsBridgeBuilder(IServiceCollection services) => Services = services;
}
