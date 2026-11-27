using SmsBridge.Abstractions;
using SmsBridge.Providers.Exotel;

namespace SmsBridge.ProviderTests;

public sealed class ExotelResponseMapperTests
{
    [Theory]
    [InlineData("queued", SmsDeliveryStatus.Queued, true)]
    [InlineData("sending", SmsDeliveryStatus.Accepted, true)]
    [InlineData("submitted", SmsDeliveryStatus.Sent, true)]
    [InlineData("sent", SmsDeliveryStatus.Delivered, true)]
    [InlineData("failed", SmsDeliveryStatus.Failed, false)]
    [InlineData("failed-dnd", SmsDeliveryStatus.Failed, false)]
    [InlineData("failed_dnd", SmsDeliveryStatus.Failed, false)]
    public void Response_MapsStatusAndKeepsSid(string status, SmsDeliveryStatus expected, bool success)
    {
        var result = ExotelSmsResponseMapper.FromResponse("exotel",
            $$$"""{"SMSMessage":{"Sid":"sms-123","Status":"{{{status}}}"}}""");

        result.Success.Should().Be(success);
        result.ProviderMessageId.Should().Be("sms-123");
        result.Status.Should().Be(expected);
        result.IsTransientFailure.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not JSON")]
    [InlineData("""{"SMSMessage":[]}""")]
    [InlineData("""{"SMSMessage":{"Sid":"sms-123","Status":"unexpected"}}""")]
    [InlineData("""{"SMSMessage":{"Status":"queued"}}""")]
    public void UnexpectedAcknowledgement_SuppressesFailover(string body)
    {
        var result = ExotelSmsResponseMapper.FromResponse("exotel", body);

        result.Success.Should().BeFalse();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }
}
