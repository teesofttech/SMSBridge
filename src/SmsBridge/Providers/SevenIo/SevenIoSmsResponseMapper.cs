using System.Globalization;
using SmsBridge.Abstractions;
using SmsBridge.Internal;

namespace SmsBridge.Providers.SevenIo;

internal static class SevenIoSmsResponseMapper
{
    internal static SmsSendResult FromResponse(string providerName, string body)
    {
        var code = body.Trim();
        return code switch
        {
            "100" => SmsSendResult.Succeeded(providerName, null),
            "101" => SmsSendResult.Failed(providerName, "101", "seven.io could not send to every recipient.",
                mayHaveBeenAccepted: true),
            "600" => SmsSendResult.Failed(providerName, "600", "seven.io reported an error during sending.",
                isTransient: true, mayHaveBeenAccepted: true),
            "900" => SmsSendResult.Failed(providerName, "900", "seven.io authentication failed."),
            "201" or "202" or "301" or "305" or "308" or "309" or "501" or "603" or "802" or "901" or "902" or "903" =>
                SmsSendResult.Failed(providerName, code, "seven.io rejected the SMS request."),
            _ => ProviderResponseFailures.InvalidSuccessResponse(providerName, "seven.io")
        };
    }

    internal static SmsSendResult FromHttpError(string providerName, int status)
    {
        var ambiguous = status == 408 || status >= 500;
        return SmsSendResult.Failed(providerName, status.ToString(CultureInfo.InvariantCulture),
            $"seven.io returned HTTP {status}.", isTransient: ambiguous || status == 429,
            mayHaveBeenAccepted: ambiguous);
    }
}
