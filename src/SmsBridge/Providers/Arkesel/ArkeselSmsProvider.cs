using Microsoft.Extensions.Logging;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.Providers.Arkesel;

internal sealed class ArkeselSmsProvider : ISmsProvider
{
    private readonly ArkeselOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ArkeselSmsProvider> _logger;

    public string Name { get; }
    public SmsProviderType Type => SmsProviderType.Arkesel;

    public ArkeselSmsProvider(string name, ArkeselOptions options,
        IHttpClientFactory httpClientFactory, ILogger<ArkeselSmsProvider> logger)
    {
        Name = name;
        _options = options;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        using var request = ArkeselSmsRequestMapper.CreateRequest(message, _options);
        var client = _httpClientFactory.CreateClient(HttpClientNames.Arkesel);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            _logger.LogWarning("Arkesel: HTTP request failed for provider '{Provider}'", Name);
            return SmsSendResult.Failed(Name, null, "Arkesel HTTP request failed.",
                isTransient: true, mayHaveBeenAccepted: true);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Arkesel: HTTP request timed out for provider '{Provider}'", Name);
            return SmsSendResult.Failed(Name, null, "Arkesel HTTP request timed out.",
                isTransient: true, mayHaveBeenAccepted: true);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                return ArkeselSmsResponseMapper.FromErrorResponse(Name, (int)response.StatusCode);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return ArkeselSmsResponseMapper.FromResponse(Name, message.To, body);
        }
    }
}
