using SmsBridge.Abstractions;

namespace SmsBridge.Internal;

internal static class ProviderResponseFailures
{
    public static SmsSendResult InvalidSuccessResponse(
        string providerName,
        string providerDisplayName) =>
        SmsSendResult.Failed(
            providerName,
            null,
            $"{providerDisplayName} returned a malformed or unexpected success response.",
            isTransient: false,
            mayHaveBeenAccepted: true);
}
