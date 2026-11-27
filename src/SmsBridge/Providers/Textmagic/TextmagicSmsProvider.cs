using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.Providers.Textmagic;

internal sealed class TextmagicSmsProvider(
    string name, TextmagicOptions options, IHttpClientFactory httpClientFactory) : ISmsProvider
{
    public string Name { get; } = name;
    public SmsProviderType Type => SmsProviderType.Textmagic;

    public async Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        using var request = TextmagicSmsRequestMapper.CreateRequest(message, options);
        using var client = httpClientFactory.CreateClient(HttpClientNames.Textmagic);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return response.IsSuccessStatusCode
                ? TextmagicSmsResponseMapper.FromResponse(Name, body)
                : TextmagicSmsResponseMapper.FromHttpError(Name, (int)response.StatusCode, body);
        }
        catch (HttpRequestException)
        {
            return SmsSendResult.Failed(Name, null, "Textmagic HTTP request failed.",
                isTransient: true, mayHaveBeenAccepted: true);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return SmsSendResult.Failed(Name, null, "Textmagic HTTP request timed out.",
                isTransient: true, mayHaveBeenAccepted: true);
        }
    }
}
