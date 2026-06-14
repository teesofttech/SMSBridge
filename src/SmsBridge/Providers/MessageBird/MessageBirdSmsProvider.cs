using SmsBridge.Abstractions;

namespace SmsBridge.Providers.MessageBird;

/// <summary>Planned provider — not yet implemented.</summary>
internal sealed class MessageBirdSmsProvider : ISmsProvider
{
    public string Name { get; }
    public SmsProviderType Type => SmsProviderType.MessageBird;

    public MessageBirdSmsProvider(string name) => Name = name;

    public Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default) =>
        throw new SmsBridgeException("The MessageBird provider is planned but not yet implemented in this version of SmsBridge.");
}
