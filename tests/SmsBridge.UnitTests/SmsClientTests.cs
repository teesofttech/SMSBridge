using Microsoft.Extensions.Logging;
using NSubstitute;
using SmsBridge.Abstractions;
using SmsBridge.Options;
using SmsBridge.Routing;

namespace SmsBridge.UnitTests;

public sealed class SmsClientTests
{
    [Fact]
    public async Task SendAsync_DoesNotInvokeSameProviderAsFailover()
    {
        var provider = Substitute.For<ISmsProvider>();
        provider.Name.Returns("twilio");
        provider.SendAsync(Arg.Any<SmsMessage>(), Arg.Any<CancellationToken>())
            .Returns(SmsSendResult.Failed("twilio", "429", "Rate limited", isTransient: true));

        var options = Microsoft.Extensions.Options.Options.Create(new SmsBridgeOptions
        {
            DefaultProvider = "twilio",
            EnableFailover = true,
            FailoverProvider = "TWILIO",
            Providers = new Dictionary<string, SmsProviderOptions>
            {
                ["twilio"] = new() { Type = SmsProviderType.Twilio }
            }
        });
        var router = new FailoverSmsProviderRouter(
            [provider],
            options,
            Substitute.For<ILogger<FailoverSmsProviderRouter>>());
        var client = new SmsClient(
            router,
            options,
            Substitute.For<ILogger<SmsClient>>());

        var result = await client.SendAsync(new SmsMessage
        {
            To = "+447700900001",
            Body = "Test"
        });

        result.Success.Should().BeFalse();
        await provider.Received(1).SendAsync(
            Arg.Any<SmsMessage>(),
            Arg.Any<CancellationToken>());
    }
}
