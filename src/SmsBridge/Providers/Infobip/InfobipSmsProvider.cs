using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.Providers.Infobip;

internal sealed class InfobipSmsProvider : ISmsProvider
{
    private readonly InfobipOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<InfobipSmsProvider> _logger;

    public string Name { get; }
    public SmsProviderType Type => SmsProviderType.Infobip;

    public InfobipSmsProvider(
        string name,
        InfobipOptions options,
        IHttpClientFactory httpClientFactory,
        ILogger<InfobipSmsProvider> logger)
    {
        Name = name;
        _options = options;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SmsSendResult> SendAsync(
        SmsMessage message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Infobip: sending SMS to {To} via provider '{Provider}'",
            message.To,
            Name);

        var url = $"{_options.BaseUrl}/sms/3/messages";
        var body = InfobipSmsRequestMapper.ToRequestBody(message, _options.From);

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("App", _options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var client = _httpClientFactory.CreateClient(HttpClientNames.Infobip);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "Infobip: HTTP request failed for provider '{Provider}'",
                Name);
            return SmsSendResult.Failed(
                Name,
                null,
                ex.Message,
                isTransient: true,
                mayHaveBeenAccepted: true);
        }

        using var responseLifetime = response;
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var result = InfobipSmsResponseMapper.FromResponse(Name, responseBody);
            if (result.Success)
            {
                _logger.LogInformation(
                    "Infobip: SMS accepted, id={MessageId}, provider='{Provider}'",
                    result.ProviderMessageId,
                    Name);
            }
            else
            {
                _logger.LogWarning(
                    "Infobip: provider returned failure status, provider='{Provider}', error={Error}",
                    Name,
                    result.ErrorMessage);
            }

            return result;
        }

        var errorResult = InfobipSmsResponseMapper.FromErrorResponse(
            Name,
            (int)response.StatusCode,
            responseBody);
        _logger.LogWarning(
            "Infobip: send failed HTTP {StatusCode}, provider='{Provider}', error={Error}",
            (int)response.StatusCode,
            Name,
            errorResult.ErrorMessage);
        return errorResult;
    }
}
