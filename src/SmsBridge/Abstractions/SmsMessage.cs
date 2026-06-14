namespace SmsBridge.Abstractions;

/// <summary>
/// Represents an outbound SMS message.
/// </summary>
public sealed class SmsMessage
{
    /// <summary>The recipient phone number in E.164 format (e.g. +447700900000).</summary>
    public required string To { get; init; }

    /// <summary>The message text body.</summary>
    public required string Body { get; init; }

    /// <summary>
    /// Optional sender ID or phone number. When omitted the provider's configured From value is used.
    /// </summary>
    public string? From { get; init; }

    /// <summary>
    /// Optional provider name override. When set, this provider is used instead of the default.
    /// Must match a registered provider name in configuration.
    /// </summary>
    public string? Provider { get; init; }

    /// <summary>Optional arbitrary key-value metadata passed through to the provider where supported.</summary>
    public IDictionary<string, string>? Metadata { get; init; }
}
