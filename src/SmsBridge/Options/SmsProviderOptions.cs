using SmsBridge.Abstractions;

namespace SmsBridge.Options;

/// <summary>
/// Base options for any SMS provider entry in configuration.
/// Provider-specific settings are stored in <see cref="Settings"/>.
/// </summary>
public sealed class SmsProviderOptions
{
    /// <summary>The provider type. Must match a known <see cref="SmsProviderType"/>.</summary>
    public SmsProviderType Type { get; set; }

    /// <summary>
    /// Typed provider settings. When binding from configuration the correct concrete type
    /// is resolved based on <see cref="SmsProviderOptions.Type"/> during DI setup.
    /// </summary>
    public Dictionary<string, string> Settings { get; set; } = [];
}
