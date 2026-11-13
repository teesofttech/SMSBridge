using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmsBridge.Abstractions;
using SmsBridge.Observability;
using SmsBridge.Options;
using SmsBridge.Routing;

namespace SmsBridge.UnitTests;

public sealed class DiagnosticsPrivacyTests
{
    [Fact]
    public async Task SendAsync_DiagnosticsExcludeRecipientData()
    {
        const string recipient = "+447700900001";
        var provider = Substitute.For<ISmsProvider>();
        provider.Name.Returns("twilio");
        provider.SendAsync(Arg.Any<SmsMessage>(), Arg.Any<CancellationToken>())
            .Returns(SmsSendResult.Succeeded("twilio", "SM123"));
        var options = Microsoft.Extensions.Options.Options.Create(new SmsBridgeOptions
        {
            DefaultProvider = "twilio",
            Providers = new Dictionary<string, SmsProviderOptions>
            {
                ["twilio"] = new() { Type = SmsProviderType.Twilio }
            }
        });
        var router = new FailoverSmsProviderRouter(
            [provider],
            options,
            Substitute.For<ILogger<FailoverSmsProviderRouter>>());
        var logger = new RecordingLogger<SmsClient>();
        var client = new SmsClient(router, options, logger);
        Activity? stoppedActivity = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == SmsBridgeTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => stoppedActivity = activity
        };
        ActivitySource.AddActivityListener(listener);

        var result = await client.SendAsync(new SmsMessage { To = recipient, Body = "Hi" });

        result.Success.Should().BeTrue();
        logger.Messages.Should().NotContain(message => message.Contains(recipient, StringComparison.Ordinal));
        logger.Messages.Should().Contain(message => message.Contains("twilio", StringComparison.Ordinal));
        logger.Messages.Should().Contain(message => message.Contains("SM123", StringComparison.Ordinal));
        stoppedActivity.Should().NotBeNull();
        stoppedActivity!.Tags.Should().Contain(tag => tag.Key == "sms.provider" && tag.Value == "twilio");
        stoppedActivity.Tags.Should().NotContain(tag => tag.Key == "sms.to.hash");
        stoppedActivity.Tags.Should().NotContain(tag =>
            tag.Value != null && tag.Value.Contains(recipient, StringComparison.Ordinal));
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }
}
