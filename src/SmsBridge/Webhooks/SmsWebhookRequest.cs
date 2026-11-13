namespace SmsBridge.Webhooks;

/// <summary>The unmodified HTTP data needed to authenticate a provider webhook.</summary>
public sealed class SmsWebhookRequest
{
    private readonly byte[] _body;
    private readonly Dictionary<string, string> _headers;
    private readonly Dictionary<string, string>? _parameters;

    public SmsWebhookRequest(
        string url,
        string method,
        ReadOnlyMemory<byte> body,
        IEnumerable<KeyValuePair<string, string>> headers,
        IEnumerable<KeyValuePair<string, string>>? parameters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentNullException.ThrowIfNull(headers);

        Url = url;
        Method = method;
        _body = body.ToArray();
        _headers = new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
        _parameters = parameters is null
            ? null
            : new Dictionary<string, string>(parameters, StringComparer.Ordinal);
    }

    /// <summary>The exact public URL configured with the provider, including its query string.</summary>
    public string Url { get; }

    public string Method { get; }

    /// <summary>The raw, unmodified request body.</summary>
    public ReadOnlyMemory<byte> Body => _body;

    public IReadOnlyDictionary<string, string> Headers => _headers;

    /// <summary>Decoded form or query values, when the provider uses a flat payload.</summary>
    public IReadOnlyDictionary<string, string>? Parameters => _parameters;

    internal bool TryGetHeader(string name, out string value) =>
        _headers.TryGetValue(name, out value!);

    internal IDictionary<string, string> GetRequiredParameters()
    {
        if (_parameters is null)
            throw new SmsWebhookValidationException("Webhook parameters are required for this provider.");

        return _parameters;
    }
}
