using System.Globalization;
using System.Security.Cryptography;

namespace SmsBridge.Webhooks;

internal static class WebhookSignatureHelpers
{
    internal static void RequireValidBase64Signature(
        string suppliedSignature,
        ReadOnlySpan<byte> expectedSignature)
    {
        byte[] suppliedBytes;
        try
        {
            suppliedBytes = Convert.FromBase64String(suppliedSignature);
        }
        catch (FormatException)
        {
            throw InvalidSignature();
        }

        if (!CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedSignature))
            throw InvalidSignature();
    }

    internal static void RequireValidHexSignature(
        string suppliedSignature,
        ReadOnlySpan<byte> expectedSignature)
    {
        byte[] suppliedBytes;
        try
        {
            suppliedBytes = Convert.FromHexString(suppliedSignature);
        }
        catch (FormatException)
        {
            throw InvalidSignature();
        }

        if (!CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedSignature))
            throw InvalidSignature();
    }

    internal static long RequireFreshUnixTimestamp(
        string timestamp,
        TimeSpan replayWindow,
        TimeProvider timeProvider)
    {
        if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
            throw new SmsWebhookValidationException("The webhook timestamp is invalid.");

        DateTimeOffset sentAt;
        try
        {
            sentAt = DateTimeOffset.FromUnixTimeSeconds(seconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new SmsWebhookValidationException("The webhook timestamp is invalid.");
        }

        if ((timeProvider.GetUtcNow() - sentAt).Duration() > replayWindow)
            throw new SmsWebhookValidationException("The webhook timestamp is outside the allowed replay window.");

        return seconds;
    }

    internal static TimeSpan ValidateReplayWindow(TimeSpan replayWindow)
    {
        if (replayWindow <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(replayWindow), "The replay window must be positive.");

        return replayWindow;
    }

    internal static SmsWebhookValidationException MissingHeader(string name) =>
        new($"The required webhook header '{name}' is missing.");

    internal static SmsWebhookValidationException InvalidSignature() =>
        new("The webhook signature is invalid.");
}
