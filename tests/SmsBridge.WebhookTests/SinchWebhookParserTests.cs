using SmsBridge.Abstractions;
using SmsBridge.Webhooks;

namespace SmsBridge.WebhookTests;

public sealed class SinchWebhookParserTests
{
    [Fact]
    public void ParseJson_MapsPerRecipientDeliveryReport()
    {
        const string json = """
            {
                "type": "recipient_delivery_report_sms",
                "batch_id": "batch-123",
                "recipient": "447700900001",
                "code": 0,
                "status": "Delivered",
                "at": "2026-06-23T10:15:00Z"
            }
            """;

        var evt = new SinchWebhookParser().ParseJson(json);

        evt.Provider.Should().Be(SmsProviderType.Sinch);
        evt.MessageId.Should().Be("batch-123");
        evt.To.Should().Be("447700900001");
        evt.Status.Should().Be(SmsDeliveryStatus.Delivered);
        evt.ErrorCode.Should().Be("0");
    }
}
