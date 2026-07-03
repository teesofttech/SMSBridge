using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.Providers.Termii;

internal sealed class TermiiSmsProvider : ISmsProvider
{
    private readonly TermiiOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TermiiSmsProvider> _logger;

    public string Name { get; }
    public SmsProviderType Type => SmsProviderType.Termii;

    public TermiiSmsProvider(
        string name,
        TermiiOptions options,
        IHttpClientFactory httpClientFactory,
        ILogger<TermiiSmsProvider> logger)
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
            "Termii: sending SMS to {To} via provider '{Provider}'",
            message.To,
            Name);

        var url = $"{_options.BaseUrl}/api/sms/send";
        var body = TermiiSmsRequestMapper.ToRequestBody(message, _options);

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var client = _httpClientFactory.CreateClient(HttpClientNames.Termii);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "Termii: HTTP request failed for provider '{Provider}'",
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
            var result = TermiiSmsResponseMapper.FromResponse(Name, responseBody);
            if (result.Success)
            {
                _logger.LogInformation(
                    "Termii: SMS accepted, id={MessageId}, provider='{Provider}'",
                    result.ProviderMessageId,
                    Name);
            }
            else
            {
                _logger.LogWarning(
                    "Termii: provider returned failure status, provider='{Provider}', error={Error}",
                    Name,
                    result.ErrorMessage);
            }

            return result;
        }

        var errorResult = TermiiSmsResponseMapper.FromErrorResponse(
            Name,
            (int)response.StatusCode,
            responseBody);
        _logger.LogWarning(
            "Termii: send failed HTTP {StatusCode}, provider='{Provider}', error={Error}",
            (int)response.StatusCode,
            Name,
            errorResult.ErrorMessage);
        return errorResult;
    }
}
