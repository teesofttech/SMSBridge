namespace SmsBridge.Abstractions;

/// <summary>
/// The result of an SMS send operation.
/// </summary>
public sealed class SmsSendResult
{
    public bool Success { get; init; }

    /// <summary>The name of the provider that processed this message.</summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>The provider-assigned message ID, if available.</summary>
    public string? ProviderMessageId { get; init; }

    public SmsDeliveryStatus Status { get; init; }

    /// <summary>Provider-specific error code on failure.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Human-readable error description on failure.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// When true the failure is considered transient and failover may be attempted.
    /// </summary>
    public bool IsTransientFailure { get; init; }

    public static SmsSendResult Succeeded(string provider, string? messageId, SmsDeliveryStatus status = SmsDeliveryStatus.Accepted) =>
        new()
        {
            Success = true,
            Provider = provider,
            ProviderMessageId = messageId,
            Status = status
        };

    public static SmsSendResult Failed(
        string provider,
        string? errorCode,
        string? errorMessage,
        bool isTransient = false,
        SmsDeliveryStatus status = SmsDeliveryStatus.Failed) =>
        new()
        {
            Success = false,
            Provider = provider,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            IsTransientFailure = isTransient,
            Status = status
        };
}
