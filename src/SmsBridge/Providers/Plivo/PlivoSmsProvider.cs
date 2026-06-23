using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.Providers.Plivo;

internal sealed class PlivoSmsProvider : ISmsProvider
{
    private readonly PlivoOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PlivoSmsProvider> _logger;

    public string Name { get; }
    public SmsProviderType Type => SmsProviderType.Plivo;

    public PlivoSmsProvider(
        string name,
        PlivoOptions options,
        IHttpClientFactory httpClientFactory,
        ILogger<PlivoSmsProvider> logger)
    {
        Name = name;
        _options = options;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Plivo: sending SMS to {To} via provider '{Provider}'", message.To, Name);

        var url = $"https://api.plivo.com/v1/Account/{_options.AuthId}/Message/";
        var body = PlivoSmsRequestMapper.ToRequestBody(message, _options);

        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.AuthId}:{_options.AuthToken}"));

        var client = _httpClientFactory.CreateClient(HttpClientNames.Plivo);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Plivo: HTTP request failed for provider '{Provider}'", Name);
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
            var result = PlivoSmsResponseMapper.FromResponse(Name, responseBody);
            _logger.LogInformation("Plivo: SMS accepted, id={MessageId}, provider='{Provider}'", result.ProviderMessageId, Name);
            return result;
        }

        var errorResult = PlivoSmsResponseMapper.FromErrorResponse(Name, (int)response.StatusCode, responseBody);
        _logger.LogWarning("Plivo: send failed HTTP {StatusCode}, provider='{Provider}', error={Error}",
            (int)response.StatusCode, Name, errorResult.ErrorMessage);
        return errorResult;
    }
}
