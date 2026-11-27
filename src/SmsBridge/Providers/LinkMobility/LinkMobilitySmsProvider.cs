using System.Net;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.Providers.LinkMobility;

internal sealed class LinkMobilitySmsProvider(
    string name, LinkMobilityOptions options, IHttpClientFactory httpClientFactory) : ISmsProvider
{
    public string Name { get; } = name;
    public SmsProviderType Type => SmsProviderType.LinkMobility;

    public async Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        using var request = LinkMobilitySmsRequestMapper.CreateRequest(message, options);
        using var client = httpClientFactory.CreateClient(HttpClientNames.LinkMobility);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NoContent)
                return SmsSendResult.Succeeded(Name, null);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return response.IsSuccessStatusCode
                ? LinkMobilitySmsResponseMapper.FromResponse(Name, body)
                : LinkMobilitySmsResponseMapper.FromHttpError(Name, (int)response.StatusCode, body);
        }
        catch (HttpRequestException)
        {
            return SmsSendResult.Failed(Name, null, "LINK Mobility HTTP request failed.",
                isTransient: true, mayHaveBeenAccepted: true);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return SmsSendResult.Failed(Name, null, "LINK Mobility HTTP request timed out.",
                isTransient: true, mayHaveBeenAccepted: true);
        }
    }
}
