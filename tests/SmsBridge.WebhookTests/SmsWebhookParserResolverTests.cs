using SmsBridge.Abstractions;
using SmsBridge.Webhooks;

namespace SmsBridge.WebhookTests;

public sealed class SmsWebhookParserResolverTests
{
    [Fact]
    public void Resolve_WithDuplicateProviderParsers_ReturnsLastRegistered()
    {
        var first = new FakeParser(SmsProviderType.Twilio);
        var second = new FakeParser(SmsProviderType.Twilio);

        var resolver = new SmsWebhookParserResolver([first, second]);

        resolver.Resolve(SmsProviderType.Twilio).Should().BeSameAs(second);
    }

    [Fact]
    public void Resolve_UnknownProvider_ThrowsSmsBridgeException()
    {
        var resolver = new SmsWebhookParserResolver([new FakeParser(SmsProviderType.Twilio)]);

        var act = () => resolver.Resolve(SmsProviderType.Vonage);

        act.Should().Throw<SmsBridgeException>().WithMessage("*No webhook parser is registered*");
    }

    private sealed class FakeParser(SmsProviderType provider) : ISmsWebhookParser
    {
        public SmsProviderType Provider { get; } = provider;

        public SmsWebhookEvent Parse(IDictionary<string, string> payload) =>
            throw new NotSupportedException();
    }
}
