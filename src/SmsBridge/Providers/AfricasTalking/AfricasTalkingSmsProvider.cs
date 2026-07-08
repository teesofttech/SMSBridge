using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.Providers.AfricasTalking;

internal sealed class AfricasTalkingSmsProvider : ISmsProvider
{
    private const string SendSmsUrl = "https://api.africastalking.com/version1/messaging";

    private readonly AfricasTalkingOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AfricasTalkingSmsProvider> _logger;

    public string Name { get; }
    public SmsProviderType Type => SmsProviderType.AfricasTalking;

    public AfricasTalkingSmsProvider(
        string name,
        AfricasTalkingOptions options,
        IHttpClientFactory httpClientFactory,
        ILogger<AfricasTalkingSmsProvider> logger)
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
            "Africa's Talking: sending SMS to {To} via provider '{Provider}'",
            message.To,
            Name);

        var body = AfricasTalkingSmsRequestMapper.ToRequestBody(message, _options);
        using var request = new HttpRequestMessage(HttpMethod.Post, SendSmsUrl)
        {
            Content = new FormUrlEncodedContent(body)
        };
        request.Headers.Add("apiKey", _options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var client = _httpClientFactory.CreateClient(HttpClientNames.AfricasTalking);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "Africa's Talking: HTTP request failed for provider '{Provider}'",
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
            var result = AfricasTalkingSmsResponseMapper.FromResponse(Name, responseBody);
            if (result.Success)
            {
                _logger.LogInformation(
                    "Africa's Talking: SMS accepted, id={MessageId}, provider='{Provider}'",
                    result.ProviderMessageId,
                    Name);
            }
            else
            {
                _logger.LogWarning(
                    "Africa's Talking: provider returned failure status, provider='{Provider}', error={Error}",
                    Name,
                    result.ErrorMessage);
            }

            return result;
        }

        var errorResult = AfricasTalkingSmsResponseMapper.FromErrorResponse(
            Name,
            (int)response.StatusCode,
            responseBody);
        _logger.LogWarning(
            "Africa's Talking: send failed HTTP {StatusCode}, provider='{Provider}', error={Error}",
            (int)response.StatusCode,
            Name,
            errorResult.ErrorMessage);
        return errorResult;
    }
}
