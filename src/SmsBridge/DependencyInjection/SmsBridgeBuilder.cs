using Microsoft.Extensions.DependencyInjection;
using SmsBridge.Abstractions;

namespace SmsBridge.DependencyInjection;

/// <summary>
/// Builder returned from <c>AddSmsBridge</c>.
/// Use it to register providers via <c>UseTwilio</c>, <c>UseVonage</c>, etc.
/// </summary>
public sealed class SmsBridgeBuilder
{
    private readonly Dictionary<string, ProviderRegistration> _providerRegistrations =
        new(StringComparer.OrdinalIgnoreCase);
    private bool _registeringFromConfiguration;

    internal IServiceCollection Services { get; }

    internal SmsBridgeBuilder(IServiceCollection services) => Services = services;

    internal void RegisterConfiguredProvider(Action register)
    {
        _registeringFromConfiguration = true;
        try
        {
            register();
        }
        finally
        {
            _registeringFromConfiguration = false;
        }
    }

    internal void AddProvider(string name, Func<IServiceProvider, ISmsProvider> factory)
    {
        var source = _registeringFromConfiguration
            ? ProviderRegistrationSource.Configuration
            : ProviderRegistrationSource.Explicit;

        if (_providerRegistrations.TryGetValue(name, out var existing))
        {
            if (existing.Source == ProviderRegistrationSource.Configuration &&
                source == ProviderRegistrationSource.Explicit)
            {
                Services.Remove(existing.Descriptor);
            }
            else
            {
                var sourceDescription = existing.Source == ProviderRegistrationSource.Explicit
                    ? "explicitly"
                    : "from configuration";
                throw new SmsBridgeException(
                    $"SMS provider '{name}' has already been registered {sourceDescription}. " +
                    "Provider names are case-insensitive.");
            }
        }

        var descriptor = ServiceDescriptor.Singleton<ISmsProvider>(factory);
        Services.Add(descriptor);
        _providerRegistrations[name] = new ProviderRegistration(descriptor, source);
    }

    private sealed record ProviderRegistration(
        ServiceDescriptor Descriptor,
        ProviderRegistrationSource Source);

    private enum ProviderRegistrationSource
    {
        Configuration,
        Explicit
    }
}
