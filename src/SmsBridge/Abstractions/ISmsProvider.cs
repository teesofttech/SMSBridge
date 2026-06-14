namespace SmsBridge.Abstractions;

/// <summary>
/// Internal contract for SMS provider adapters.
/// Do not use this interface in application code — use <see cref="ISmsClient"/> instead.
/// </summary>
internal interface ISmsProvider
{
    string Name { get; }

    SmsProviderType Type { get; }

    Task<SmsSendResult> SendAsync(
        SmsMessage message,
        CancellationToken cancellationToken = default);
}
