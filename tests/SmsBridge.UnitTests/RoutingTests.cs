using Microsoft.Extensions.Options;
using NSubstitute;
using SmsBridge.Abstractions;
using SmsBridge.Options;
using SmsBridge.Routing;

namespace SmsBridge.UnitTests;

public sealed class RoutingTests
{
    private static IOptions<SmsBridgeOptions> Options(Action<SmsBridgeOptions> configure)
    {
        var opts = new SmsBridgeOptions();
        configure(opts);
        return Microsoft.Extensions.Options.Options.Create(opts);
    }

    private static ISmsProvider MakeProvider(string name)
    {
        var p = Substitute.For<ISmsProvider>();
        p.Name.Returns(name);
        return p;
    }

    [Fact]
    public void DefaultRouter_ResolvesDefaultProvider()
    {
        var twilio = MakeProvider("twilio");
        var router = new DefaultSmsProviderRouter(
            [twilio],
            Options(o => o.DefaultProvider = "twilio"));

        var resolved = router.Resolve(new SmsRoutingContext
        {
            Message = new SmsMessage { To = "+1", Body = "Hi" }
        });

        resolved.Name.Should().Be("twilio");
    }

    [Fact]
    public void DefaultRouter_ResolvesExplicitProvider()
    {
        var twilio = MakeProvider("twilio");
        var vonage = MakeProvider("vonage");
        var router = new DefaultSmsProviderRouter(
            [twilio, vonage],
            Options(o => o.DefaultProvider = "twilio"));

        var resolved = router.Resolve(new SmsRoutingContext
        {
            Message = new SmsMessage { To = "+1", Body = "Hi" },
            ExplicitProvider = "vonage"
        });

        resolved.Name.Should().Be("vonage");
    }

    [Fact]
    public void DefaultRouter_ThrowsWhenProviderNotRegistered()
    {
        var router = new DefaultSmsProviderRouter(
            [],
            Options(o => o.DefaultProvider = "twilio"));

        var act = () => router.Resolve(new SmsRoutingContext
        {
            Message = new SmsMessage { To = "+1", Body = "Hi" }
        });

        act.Should().Throw<SmsBridgeException>().WithMessage("*not registered*");
    }

    [Fact]
    public void DefaultRouter_ThrowsWhenNoDefaultProvider()
    {
        var router = new DefaultSmsProviderRouter(
            [],
            Options(o => o.DefaultProvider = ""));

        var act = () => router.Resolve(new SmsRoutingContext
        {
            Message = new SmsMessage { To = "+1", Body = "Hi" }
        });

        act.Should().Throw<SmsBridgeException>().WithMessage("*No provider could be resolved*");
    }
}
