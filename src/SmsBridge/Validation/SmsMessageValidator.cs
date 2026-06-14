using SmsBridge.Abstractions;

namespace SmsBridge.Validation;

internal static class SmsMessageValidator
{
    public static void Validate(SmsMessage message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(message.To))
            errors.Add("'To' is required.");

        if (string.IsNullOrWhiteSpace(message.Body))
            errors.Add("'Body' is required.");

        if (errors.Count > 0)
            throw new SmsBridgeException($"SMS message validation failed: {string.Join(" ", errors)}");
    }
}
