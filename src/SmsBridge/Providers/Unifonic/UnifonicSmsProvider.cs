using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.Providers.Unifonic;

internal sealed class UnifonicSmsProvider : ISmsProvider
{
    private const string SendSmsUrl = "https://el.cloud.unifonic.com/rest/SMS/messages";

    private readonly UnifonicOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UnifonicSmsProvider> _logger;

    public string Name { get; }
    public SmsProviderType Type => SmsProviderType.Unifonic;

    public UnifonicSmsProvider(
        string name,
        UnifonicOptions options,
        IHttpClientFactory httpClientFactory,
        ILogger<UnifonicSmsProvider> logger)
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
            "Unifonic: sending SMS to {To} via provider '{Provider}'",
            message.To,
            Name);

        var fields = UnifonicSmsRequestMapper.ToFormFields(message, _options);
        using var request = new HttpRequestMessage(HttpMethod.Post, SendSmsUrl)
        {
            Content = new FormUrlEncodedContent(fields)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var client = _httpClientFactory.CreateClient(HttpClientNames.Unifonic);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "Unifonic: HTTP request failed for provider '{Provider}'",
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
            var result = UnifonicSmsResponseMapper.FromResponse(Name, responseBody);
            if (result.Success)
            {
                _logger.LogInformation(
                    "Unifonic: SMS accepted, id={MessageId}, provider='{Provider}'",
                    result.ProviderMessageId,
                    Name);
            }
            else
            {
                _logger.LogWarning(
                    "Unifonic: provider returned failure status, provider='{Provider}', error={Error}",
                    Name,
                    result.ErrorMessage);
            }

            return result;
        }

        var errorResult = UnifonicSmsResponseMapper.FromErrorResponse(
            Name,
            (int)response.StatusCode,
            responseBody);
        _logger.LogWarning(
            "Unifonic: send failed HTTP {StatusCode}, provider='{Provider}', error={Error}",
            (int)response.StatusCode,
            Name,
            errorResult.ErrorMessage);
        return errorResult;
    }
}
