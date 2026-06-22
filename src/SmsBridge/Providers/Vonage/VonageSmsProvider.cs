using Microsoft.Extensions.Logging;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.Providers.Vonage;

internal sealed class VonageSmsProvider : ISmsProvider
{
    private readonly VonageOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<VonageSmsProvider> _logger;

    public string Name { get; }
    public SmsProviderType Type => SmsProviderType.Vonage;

    public VonageSmsProvider(
        string name,
        VonageOptions options,
        IHttpClientFactory httpClientFactory,
        ILogger<VonageSmsProvider> logger)
    {
        Name = name;
        _options = options;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Vonage: sending SMS to {To} via provider '{Provider}'", message.To, Name);

        const string url = "https://rest.nexmo.com/sms/json";
        var fields = VonageSmsRequestMapper.ToFormFields(message, _options);
        using var content = new FormUrlEncodedContent(fields);

        var client = _httpClientFactory.CreateClient(HttpClientNames.Vonage);

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(url, content, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Vonage: HTTP request failed for provider '{Provider}'", Name);
            return SmsSendResult.Failed(Name, null, ex.Message, isTransient: true);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var result = VonageSmsResponseMapper.FromResponse(Name, responseBody);
            if (result.Success)
                _logger.LogInformation("Vonage: SMS accepted, messageId={MessageId}, provider='{Provider}'", result.ProviderMessageId, Name);
            else
                _logger.LogWarning("Vonage: provider returned failure, provider='{Provider}', error={Error}", Name, result.ErrorMessage);
            return result;
        }

        var errorResult = VonageSmsResponseMapper.FromErrorResponse(Name, (int)response.StatusCode, responseBody);
        _logger.LogWarning("Vonage: send failed HTTP {StatusCode}, provider='{Provider}', error={Error}",
            (int)response.StatusCode, Name, errorResult.ErrorMessage);
        return errorResult;
    }
}
