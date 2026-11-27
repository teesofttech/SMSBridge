using Microsoft.Extensions.Logging;
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DefaultRouter_BlankExplicitProvider_FallsBackToDefault(string? explicitProvider)
    {
        var router = new DefaultSmsProviderRouter(
            [MakeProvider("twilio"), MakeProvider("vonage")],
            Options(o => o.DefaultProvider = "twilio"));

        var resolved = router.Resolve(new SmsRoutingContext
        {
            Message = new SmsMessage { To = "+1", Body = "Hi" },
            ExplicitProvider = explicitProvider
        });

        resolved.Name.Should().Be("twilio");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FailoverRouter_BlankExplicitProvider_FallsBackToDefault(string? explicitProvider)
    {
        var router = MakeFailoverRouter("twilio", MakeProvider("twilio"), MakeProvider("vonage"));

        var resolved = router.Resolve(new SmsRoutingContext
        {
            Message = new SmsMessage { To = "+1", Body = "Hi" },
            ExplicitProvider = explicitProvider
        });

        resolved.Name.Should().Be("twilio");
    }

    [Fact]
    public void FailoverRouter_ResolvesExplicitProvider()
    {
        var router = MakeFailoverRouter("twilio", MakeProvider("twilio"), MakeProvider("vonage"));

        var resolved = router.Resolve(new SmsRoutingContext
        {
            Message = new SmsMessage { To = "+1", Body = "Hi" },
            ExplicitProvider = "vonage"
        });

        resolved.Name.Should().Be("vonage");
    }

    [Fact]
    public void FailoverRouter_ThrowsWhenExplicitProviderNotRegistered()
    {
        var router = MakeFailoverRouter("twilio", MakeProvider("twilio"));

        var act = () => router.Resolve(new SmsRoutingContext
        {
            Message = new SmsMessage { To = "+1", Body = "Hi" },
            ExplicitProvider = "unknown"
        });

        act.Should().Throw<SmsBridgeException>().WithMessage("*not registered*");
    }

    private static FailoverSmsProviderRouter MakeFailoverRouter(
        string defaultProvider,
        params ISmsProvider[] providers) =>
        new(
            providers,
            Options(o => o.DefaultProvider = defaultProvider),
            Substitute.For<ILogger<FailoverSmsProviderRouter>>());
}
