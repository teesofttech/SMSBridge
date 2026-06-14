namespace SmsBridge.Abstractions;

/// <summary>
/// The primary interface for sending SMS messages through SMSBridge.
/// Application code should depend only on this interface.
/// </summary>
public interface ISmsClient
{
    /// <summary>
    /// Sends an SMS message using the configured provider.
    /// </summary>
    Task<SmsSendResult> SendAsync(
        SmsMessage message,
        CancellationToken cancellationToken = default);
}
