using SmsBridge.Options;
using SmsBridge.Providers.AwsSns;

namespace SmsBridge.ProviderTests;

public sealed class AwsSnsRequestSignerTests
{
    [Fact]
    public void Sign_AddsExpectedAuthorizationHeaderForFixedPublishPayload()
    {
        var options = new AwsSnsOptions
        {
            AccessKeyId = "AKIDEXAMPLE",
            SecretAccessKey = "wJalrXUtnFEMI/K7MDENG+bPxRfiCYEXAMPLEKEY",
            Region = "eu-west-2"
        };
        const string payload = "Action=Publish&Version=2010-03-31&PhoneNumber=%2B447700900001&Message=Hi";
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://sns.eu-west-2.amazonaws.com/");

        AwsSnsRequestSigner.Sign(
            request,
            options,
            new DateTimeOffset(2026, 7, 6, 12, 0, 0, TimeSpan.Zero),
            payload);

        request.Headers.TryGetValues("Authorization", out var authorizationValues)
            .Should()
            .BeTrue();
        authorizationValues!.Single().Should().Be(
            "AWS4-HMAC-SHA256 Credential=AKIDEXAMPLE/20260706/eu-west-2/sns/aws4_request, SignedHeaders=host;x-amz-date, Signature=74885e5032157af7654ffdbb4c9efef7cbf39caf94ca8fb5cc61be95472731b0");
        request.Headers.TryGetValues("x-amz-date", out var dateValues)
            .Should()
            .BeTrue();
        dateValues!.Single().Should().Be("20260706T120000Z");
    }
}
