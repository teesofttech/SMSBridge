using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.Providers.Esendex;

internal sealed class EsendexSmsProvider(
    string name, EsendexOptions options, IHttpClientFactory httpClientFactory) : ISmsProvider
{
    public string Name { get; } = name;
    public SmsProviderType Type => SmsProviderType.Esendex;

    public async Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        using var request = EsendexSmsRequestMapper.CreateRequest(message, options);
        using var client = httpClientFactory.CreateClient(HttpClientNames.Esendex);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return EsendexSmsResponseMapper.FromHttpError(Name, (int)response.StatusCode);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return EsendexSmsResponseMapper.FromResponse(Name, body);
        }
        catch (HttpRequestException)
        {
            return SmsSendResult.Failed(Name, null, "Esendex HTTP request failed.",
                isTransient: true, mayHaveBeenAccepted: true);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return SmsSendResult.Failed(Name, null, "Esendex HTTP request timed out.",
                isTransient: true, mayHaveBeenAccepted: true);
        }
    }
}
