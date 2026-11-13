using SmsBridge.Abstractions;
using SmsBridge.Providers.AfricasTalking;
using SmsBridge.Providers.AwsSns;
using SmsBridge.Providers.Infobip;
using SmsBridge.Providers.MessageBird;
using SmsBridge.Providers.Plivo;
using SmsBridge.Providers.Sinch;
using SmsBridge.Providers.SmartSms;
using SmsBridge.Providers.Telnyx;
using SmsBridge.Providers.Termii;
using SmsBridge.Providers.Twilio;
using SmsBridge.Providers.Unifonic;
using SmsBridge.Providers.Vonage;

namespace SmsBridge.ProviderTests;

public sealed class MalformedSuccessResponseTests
{
    [Theory]
    [InlineData("twilio", "Twilio")]
    [InlineData("vonage", "Vonage")]
    [InlineData("aws", "AWS SNS")]
    [InlineData("messagebird", "MessageBird")]
    [InlineData("infobip", "Infobip")]
    [InlineData("termii", "Termii")]
    [InlineData("smart-sms", "SmartSMSSolutions")]
    [InlineData("sinch", "Sinch")]
    [InlineData("plivo", "Plivo")]
    [InlineData("telnyx", "Telnyx")]
    [InlineData("unifonic", "Unifonic")]
    [InlineData("africas-talking", "Africa's Talking")]
    public void FromResponse_ReturnsSafeFailureForInvalidSuccessBodies(
        string providerName,
        string providerDisplayName)
    {
        var bodies = new[]
        {
            string.Empty,
            "{",
            "{}",
            SchemaVariantBody(providerName)
        };

        foreach (var body in bodies)
        {
            var result = Map(providerName, body);

            result.Success.Should().BeFalse();
            result.Provider.Should().Be(providerName);
            result.ErrorMessage.Should().ContainEquivalentOf(providerDisplayName);
            result.IsTransientFailure.Should().BeFalse();
            result.MayHaveBeenAccepted.Should().BeTrue();
        }
    }

    private static SmsSendResult Map(string providerName, string body) => providerName switch
    {
        "twilio" => TwilioSmsResponseMapper.FromResponse(providerName, body),
        "vonage" => VonageSmsResponseMapper.FromResponse(providerName, body),
        "aws" => AwsSnsSmsResponseMapper.FromResponse(providerName, body),
        "messagebird" => MessageBirdSmsResponseMapper.FromResponse(providerName, body),
        "infobip" => InfobipSmsResponseMapper.FromResponse(providerName, body),
        "termii" => TermiiSmsResponseMapper.FromResponse(providerName, body),
        "smart-sms" => SmartSmsSmsResponseMapper.FromResponse(providerName, body),
        "sinch" => SinchSmsResponseMapper.FromResponse(providerName, body),
        "plivo" => PlivoSmsResponseMapper.FromResponse(providerName, body),
        "telnyx" => TelnyxSmsResponseMapper.FromResponse(providerName, body),
        "unifonic" => UnifonicSmsResponseMapper.FromResponse(providerName, body),
        "africas-talking" => AfricasTalkingSmsResponseMapper.FromResponse(providerName, body),
        _ => throw new ArgumentOutOfRangeException(nameof(providerName), providerName, null)
    };

    private static string SchemaVariantBody(string providerName) => providerName switch
    {
        "twilio" => """{"status":123}""",
        "vonage" => """{"messages":{}}""",
        "aws" => "<PublishResponse />",
        "messagebird" => "[]",
        "infobip" => """{"messages":[1]}""",
        "termii" => "[]",
        "smart-sms" => """{"errors":[1]}""",
        "sinch" => """{"id":123}""",
        "plivo" => """{"message_uuid":[123]}""",
        "telnyx" => """{"data":{"id":123}}""",
        "unifonic" => "[]",
        "africas-talking" => "[]",
        _ => throw new ArgumentOutOfRangeException(nameof(providerName), providerName, null)
    };
}
