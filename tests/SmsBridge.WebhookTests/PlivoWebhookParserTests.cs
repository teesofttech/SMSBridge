using SmsBridge.Abstractions;
using SmsBridge.Webhooks;

namespace SmsBridge.WebhookTests;

public sealed class PlivoWebhookParserTests
{
    private readonly PlivoWebhookParser _parser = new();

    [Fact]
    public void Parse_MapsDeliveredStatus()
    {
        var payload = new Dictionary<string, string>
        {
            ["MessageUUID"] = "msg-123",
            ["To"] = "+447700900001",
            ["Status"] = "delivered",
            ["ErrorCode"] = "000"
        };

        var evt = _parser.Parse(payload);

        evt.Provider.Should().Be(SmsProviderType.Plivo);
        evt.MessageId.Should().Be("msg-123");
        evt.To.Should().Be("+447700900001");
        evt.Status.Should().Be(SmsDeliveryStatus.Delivered);
        evt.ErrorCode.Should().Be("000");
        evt.IsTransientFailure.Should().BeFalse();
    }

    [Theory]
    [InlineData("20")]
    [InlineData("80")]
    [InlineData("300")]
    public void Parse_MarksDocumentedTemporaryErrorsAsTransient(string errorCode)
    {
        var payload = new Dictionary<string, string>
        {
            ["MessageUUID"] = "msg-456",
            ["Status"] = "undelivered",
            ["ErrorCode"] = errorCode
        };

        var evt = _parser.Parse(payload);

        evt.Status.Should().Be(SmsDeliveryStatus.Undelivered);
        evt.ErrorCode.Should().Be(errorCode);
        evt.IsTransientFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("40")]
    [InlineData("50")]
    [InlineData("70")]
    [InlineData("200")]
    [InlineData("910")]
    public void Parse_MarksPermanentOrActionableErrorsAsNonTransient(string errorCode)
    {
        var payload = new Dictionary<string, string>
        {
            ["Status"] = "failed",
            ["ErrorCode"] = errorCode
        };

        var evt = _parser.Parse(payload);

        evt.Status.Should().Be(SmsDeliveryStatus.Failed);
        evt.ErrorCode.Should().Be(errorCode);
        evt.IsTransientFailure.Should().BeFalse();
    }
}
