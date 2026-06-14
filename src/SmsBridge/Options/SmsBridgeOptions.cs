namespace SmsBridge.Options;

public sealed class SmsBridgeOptions
{
    public const string SectionName = "SmsBridge";

    /// <summary>The name of the default provider. Must match a key in <see cref="Providers"/>.</summary>
    public string DefaultProvider { get; set; } = string.Empty;

    /// <summary>When true and the primary provider returns a transient failure, the failover provider is tried.</summary>
    public bool EnableFailover { get; set; }

    /// <summary>The name of the failover provider. Must match a key in <see cref="Providers"/> when failover is enabled.</summary>
    public string? FailoverProvider { get; set; }

    /// <summary>Named provider configurations keyed by provider name.</summary>
    public Dictionary<string, SmsProviderOptions> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
