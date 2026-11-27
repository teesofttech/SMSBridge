using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.Providers.Exotel;

internal sealed class ExotelSmsProvider(
    string name, ExotelOptions options, IHttpClientFactory httpClientFactory) : ISmsProvider
{
    public string Name { get; } = name;
    public SmsProviderType Type => SmsProviderType.Exotel;

    public async Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        using var request = ExotelSmsRequestMapper.CreateRequest(message, options);
        using var client = httpClientFactory.CreateClient(HttpClientNames.Exotel);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return ExotelSmsResponseMapper.FromHttpError(Name, (int)response.StatusCode);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return ExotelSmsResponseMapper.FromResponse(Name, body);
        }
        catch (HttpRequestException)
        {
            return SmsSendResult.Failed(Name, null, "Exotel HTTP request failed.",
                isTransient: true, mayHaveBeenAccepted: true);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return SmsSendResult.Failed(Name, null, "Exotel HTTP request timed out.",
                isTransient: true, mayHaveBeenAccepted: true);
        }
    }
}
