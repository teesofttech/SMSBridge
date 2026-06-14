using SmsBridge.Abstractions;

namespace SmsBridge.Providers.AwsSns;

/// <summary>Planned provider — not yet implemented.</summary>
internal sealed class AwsSnsSmsProvider : ISmsProvider
{
    public string Name { get; }
    public SmsProviderType Type => SmsProviderType.AwsSns;

    public AwsSnsSmsProvider(string name) => Name = name;

    public Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default) =>
        throw new SmsBridgeException("The AWS SNS provider is planned but not yet implemented in this version of SmsBridge.");
}
