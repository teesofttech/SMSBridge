using SmsBridge.Abstractions;
using SmsBridge.Webhooks;

namespace SmsBridge.WebhookTests;

public sealed class TelnyxWebhookParserTests
{
    [Theory]
    [InlineData("40006")]
    [InlineData("40008")]
    public void ParseJson_MapsTemporaryDeliveryFailure(string errorCode)
    {
        var json = $$"""
            {
                "data": {
                    "event_type": "message.finalized",
                    "occurred_at": "2026-06-23T10:15:00Z",
                    "payload": {
                        "id": "msg-123",
                        "to": [{
                            "phone_number": "+447700900001",
                            "status": "delivery_failed"
                        }],
                        "errors": [{
                            "code": "{{errorCode}}"
                        }]
                    }
                }
            }
            """;

        var evt = new TelnyxWebhookParser().ParseJson(json);

        evt.Provider.Should().Be(SmsProviderType.Telnyx);
        evt.MessageId.Should().Be("msg-123");
        evt.Status.Should().Be(SmsDeliveryStatus.Failed);
        evt.ErrorCode.Should().Be(errorCode);
        evt.IsTransientFailure.Should().BeTrue();
    }
}
