namespace SmsBridge.Internal.Extensions;

internal static class DictionaryExtensions
{
    public static string? GetValueOrDefault(this Dictionary<string, string> dict, string key) =>
        dict.TryGetValue(key, out var value) ? value : null;

    public static string GetRequired(this Dictionary<string, string> dict, string key, string providerName)
    {
        if (!dict.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            throw new Abstractions.SmsBridgeException(
                $"Provider '{providerName}' requires the setting '{key}' but it was not found in configuration.");

        return value;
    }
}
