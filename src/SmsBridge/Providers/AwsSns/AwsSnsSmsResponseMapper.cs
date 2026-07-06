using System.Xml.Linq;
using SmsBridge.Abstractions;

namespace SmsBridge.Providers.AwsSns;

internal static class AwsSnsSmsResponseMapper
{
    public static SmsSendResult FromResponse(string providerName, string xml)
    {
        try
        {
            var document = XDocument.Parse(xml);
            var messageId = document.Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "MessageId")
                ?.Value;

            if (string.IsNullOrWhiteSpace(messageId))
            {
                return SmsSendResult.Failed(
                    providerName,
                    null,
                    "AWS SNS response did not include a MessageId.",
                    isTransient: false);
            }

            return SmsSendResult.Succeeded(providerName, messageId, SmsDeliveryStatus.Accepted);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Xml.XmlException)
        {
            return SmsSendResult.Failed(
                providerName,
                null,
                "Unable to parse AWS SNS response.",
                isTransient: false);
        }
    }

    public static SmsSendResult FromErrorResponse(string providerName, int httpStatusCode, string xml)
    {
        var (errorCode, errorMessage) = ReadError(xml);
        var isTransient = IsTransient(httpStatusCode, errorCode);

        return SmsSendResult.Failed(
            providerName,
            errorCode,
            errorMessage ?? $"HTTP {httpStatusCode}",
            isTransient,
            mayHaveBeenAccepted: httpStatusCode >= 500);
    }

    private static (string? Code, string? Message) ReadError(string xml)
    {
        try
        {
            var document = XDocument.Parse(xml);
            var error = document.Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "Error");

            if (error is null)
                return (null, null);

            var code = error.Elements()
                .FirstOrDefault(element => element.Name.LocalName == "Code")
                ?.Value;
            var message = error.Elements()
                .FirstOrDefault(element => element.Name.LocalName == "Message")
                ?.Value;

            return (code, message);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Xml.XmlException)
        {
            return (null, null);
        }
    }

    private static bool IsTransient(int httpStatusCode, string? errorCode)
    {
        if (httpStatusCode >= 500 || httpStatusCode == 429)
            return true;

        return errorCode switch
        {
            "InternalError" or "ServiceUnavailable" or "RequestTimeout" or "RequestLimitExceeded" => true,
            not null when errorCode.Contains("Throttl", StringComparison.OrdinalIgnoreCase) => true,
            _ => false
        };
    }
}
