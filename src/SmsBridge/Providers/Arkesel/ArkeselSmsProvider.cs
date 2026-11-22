using Microsoft.Extensions.Logging;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.Providers.Arkesel;

internal sealed class ArkeselSmsProvider : ISmsProvider
{
    private readonly ArkeselOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ArkeselSmsProvider> _logger;

    public string Name { get; }
    public SmsProviderType Type => SmsProviderType.Arkesel;

    public ArkeselSmsProvider(string name, ArkeselOptions options,
        IHttpClientFactory httpClientFactory, ILogger<ArkeselSmsProvider> logger)
    {
        Name = name;
        _options = options;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        // V1 defines a GET endpoint. Do not log the URI: it contains the API key and message.
        using var request = new HttpRequestMessage(HttpMethod.Get,
            ArkeselSmsRequestMapper.ToRequestUri(message, _options));
        var client = _httpClientFactory.CreateClient(HttpClientNames.Arkesel);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            _logger.LogWarning("Arkesel: HTTP request failed for provider '{Provider}'", Name);
            return SmsSendResult.Failed(Name, null, "Arkesel HTTP request failed.",
                isTransient: true, mayHaveBeenAccepted: true);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return response.IsSuccessStatusCode
                ? ArkeselSmsResponseMapper.FromResponse(Name, body)
                : ArkeselSmsResponseMapper.FromErrorResponse(Name, (int)response.StatusCode, body);
        }
    }
}
