using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.Providers.Sinch;

internal sealed class SinchSmsProvider : ISmsProvider
{
    private readonly SinchOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SinchSmsProvider> _logger;

    public string Name { get; }
    public SmsProviderType Type => SmsProviderType.Sinch;

    public SinchSmsProvider(
        string name,
        SinchOptions options,
        IHttpClientFactory httpClientFactory,
        ILogger<SinchSmsProvider> logger)
    {
        Name = name;
        _options = options;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sinch: sending SMS to {To} via provider '{Provider}'", message.To, Name);

        var url = $"{_options.BaseUrl}/xms/v1/{_options.ServicePlanId}/batches";
        var body = SinchSmsRequestMapper.ToRequestBody(message, _options);

        var client = _httpClientFactory.CreateClient(HttpClientNames.Sinch);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiToken);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Sinch: HTTP request failed for provider '{Provider}'", Name);
            return SmsSendResult.Failed(
                Name,
                null,
                ex.Message,
                isTransient: true,
                mayHaveBeenAccepted: true);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var result = SinchSmsResponseMapper.FromResponse(Name, responseBody);
            if (result.Success)
                _logger.LogInformation("Sinch: SMS accepted, id={MessageId}, provider='{Provider}'", result.ProviderMessageId, Name);
            else
                _logger.LogWarning("Sinch: provider returned failure status, provider='{Provider}', error={Error}", Name, result.ErrorMessage);
            return result;
        }

        var errorResult = SinchSmsResponseMapper.FromErrorResponse(Name, (int)response.StatusCode, responseBody);
        _logger.LogWarning("Sinch: send failed HTTP {StatusCode}, provider='{Provider}', error={Error}",
            (int)response.StatusCode, Name, errorResult.ErrorMessage);
        return errorResult;
    }
}
