using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.Providers.Telnyx;

internal sealed class TelnyxSmsProvider : ISmsProvider
{
    private readonly TelnyxOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TelnyxSmsProvider> _logger;

    public string Name { get; }
    public SmsProviderType Type => SmsProviderType.Telnyx;

    public TelnyxSmsProvider(
        string name,
        TelnyxOptions options,
        IHttpClientFactory httpClientFactory,
        ILogger<TelnyxSmsProvider> logger)
    {
        Name = name;
        _options = options;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Telnyx: sending SMS to {To} via provider '{Provider}'", message.To, Name);

        const string url = "https://api.telnyx.com/v2/messages";
        var body = TelnyxSmsRequestMapper.ToRequestBody(message, _options.From);

        var client = _httpClientFactory.CreateClient(HttpClientNames.Telnyx);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Telnyx: HTTP request failed for provider '{Provider}'", Name);
            return SmsSendResult.Failed(Name, null, ex.Message, isTransient: true);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var result = TelnyxSmsResponseMapper.FromResponse(Name, responseBody);
            if (result.Success)
                _logger.LogInformation("Telnyx: SMS accepted, id={MessageId}, provider='{Provider}'", result.ProviderMessageId, Name);
            else
                _logger.LogWarning("Telnyx: provider returned failure status, provider='{Provider}', error={Error}", Name, result.ErrorMessage);
            return result;
        }

        var errorResult = TelnyxSmsResponseMapper.FromErrorResponse(Name, (int)response.StatusCode, responseBody);
        _logger.LogWarning("Telnyx: send failed HTTP {StatusCode}, provider='{Provider}', error={Error}",
            (int)response.StatusCode, Name, errorResult.ErrorMessage);
        return errorResult;
    }
}
