using System.Text;
using Microsoft.Extensions.Logging;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.Providers.Twilio;

internal sealed class TwilioSmsProvider : ISmsProvider
{
    private readonly TwilioOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TwilioSmsProvider> _logger;

    public string Name { get; }
    public SmsProviderType Type => SmsProviderType.Twilio;

    public TwilioSmsProvider(
        string name,
        TwilioOptions options,
        IHttpClientFactory httpClientFactory,
        ILogger<TwilioSmsProvider> logger)
    {
        Name = name;
        _options = options;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Twilio: sending SMS to {To} via provider '{Provider}'", message.To, Name);

        var url = $"https://api.twilio.com/2010-04-01/Accounts/{_options.AccountSid}/Messages.json";
        var fields = TwilioSmsRequestMapper.ToFormFields(message, _options.From);
        var content = new FormUrlEncodedContent(fields);

        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.AccountSid}:{_options.AuthToken}"));

        var client = _httpClientFactory.CreateClient(HttpClientNames.Twilio);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Twilio: HTTP request failed for provider '{Provider}'", Name);
            return SmsSendResult.Failed(Name, null, ex.Message, isTransient: true);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var result = TwilioSmsResponseMapper.FromResponse(Name, body);
            if (result.Success)
                _logger.LogInformation("Twilio: SMS accepted, sid={MessageId}, provider='{Provider}'", result.ProviderMessageId, Name);
            else
                _logger.LogWarning("Twilio: provider returned failure status, provider='{Provider}', error={Error}", Name, result.ErrorMessage);
            return result;
        }

        var errorResult = TwilioSmsResponseMapper.FromErrorResponse(Name, (int)response.StatusCode, body);
        _logger.LogWarning("Twilio: send failed HTTP {StatusCode}, provider='{Provider}', error={Error}",
            (int)response.StatusCode, Name, errorResult.ErrorMessage);
        return errorResult;
    }
}
