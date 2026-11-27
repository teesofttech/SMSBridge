using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.Providers.SevenIo;

internal sealed class SevenIoSmsProvider(
    string name, SevenIoOptions options, IHttpClientFactory httpClientFactory) : ISmsProvider
{
    public string Name { get; } = name;
    public SmsProviderType Type => SmsProviderType.SevenIo;

    public async Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        using var request = SevenIoSmsRequestMapper.CreateRequest(message, options);
        using var client = httpClientFactory.CreateClient(HttpClientNames.SevenIo);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return SevenIoSmsResponseMapper.FromHttpError(Name, (int)response.StatusCode);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return SevenIoSmsResponseMapper.FromResponse(Name, body);
        }
        catch (HttpRequestException)
        {
            return SmsSendResult.Failed(Name, null, "seven.io HTTP request failed.",
                isTransient: true, mayHaveBeenAccepted: true);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return SmsSendResult.Failed(Name, null, "seven.io HTTP request timed out.",
                isTransient: true, mayHaveBeenAccepted: true);
        }
    }
}
