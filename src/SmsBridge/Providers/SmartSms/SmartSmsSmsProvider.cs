using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.Providers.SmartSms;

internal sealed class SmartSmsSmsProvider : ISmsProvider
{
    private const string SendSmsUrl = "https://app.smartsmssolutions.com/io/api/client/v1/sms/";

    private readonly SmartSmsOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SmartSmsSmsProvider> _logger;

    public string Name { get; }
    public SmsProviderType Type => SmsProviderType.SmartSms;

    public SmartSmsSmsProvider(
        string name,
        SmartSmsOptions options,
        IHttpClientFactory httpClientFactory,
        ILogger<SmartSmsSmsProvider> logger)
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
            "SmartSMSSolutions: sending SMS to {To} via provider '{Provider}'",
            message.To,
            Name);

        var body = SmartSmsSmsRequestMapper.ToRequestBody(message, _options);
        using var request = new HttpRequestMessage(HttpMethod.Post, SendSmsUrl)
        {
            Content = new FormUrlEncodedContent(body)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var client = _httpClientFactory.CreateClient(HttpClientNames.SmartSms);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "SmartSMSSolutions: HTTP request failed for provider '{Provider}'",
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
            var result = SmartSmsSmsResponseMapper.FromResponse(Name, responseBody);
            if (result.Success)
            {
                _logger.LogInformation(
                    "SmartSMSSolutions: SMS accepted, id={MessageId}, provider='{Provider}'",
                    result.ProviderMessageId,
                    Name);
            }
            else
            {
                _logger.LogWarning(
                    "SmartSMSSolutions: provider returned failure status, provider='{Provider}', error={Error}",
                    Name,
                    result.ErrorMessage);
            }

            return result;
        }

        var errorResult = SmartSmsSmsResponseMapper.FromErrorResponse(
            Name,
            (int)response.StatusCode,
            responseBody);
        _logger.LogWarning(
            "SmartSMSSolutions: send failed HTTP {StatusCode}, provider='{Provider}', error={Error}",
            (int)response.StatusCode,
            Name,
            errorResult.ErrorMessage);
        return errorResult;
    }
}
