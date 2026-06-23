using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.Providers.MessageBird;

internal sealed class MessageBirdSmsProvider : ISmsProvider
{
    private const string MessagesUrl = "https://rest.messagebird.com/messages";

    private readonly MessageBirdOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MessageBirdSmsProvider> _logger;

    public string Name { get; }
    public SmsProviderType Type => SmsProviderType.MessageBird;

    public MessageBirdSmsProvider(
        string name,
        MessageBirdOptions options,
        IHttpClientFactory httpClientFactory,
        ILogger<MessageBirdSmsProvider> logger)
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
            "MessageBird: sending SMS to {To} via provider '{Provider}'",
            message.To,
            Name);

        var fields = MessageBirdSmsRequestMapper.ToFormFields(message, _options.From);
        using var request = new HttpRequestMessage(HttpMethod.Post, MessagesUrl)
        {
            Content = new FormUrlEncodedContent(fields)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("AccessKey", _options.AccessKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var client = _httpClientFactory.CreateClient(HttpClientNames.MessageBird);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "MessageBird: HTTP request failed for provider '{Provider}'",
                Name);
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
            var result = MessageBirdSmsResponseMapper.FromResponse(Name, responseBody);
            if (result.Success)
            {
                _logger.LogInformation(
                    "MessageBird: SMS accepted, id={MessageId}, provider='{Provider}'",
                    result.ProviderMessageId,
                    Name);
            }
            else
            {
                _logger.LogWarning(
                    "MessageBird: provider returned failure status, provider='{Provider}', error={Error}",
                    Name,
                    result.ErrorMessage);
            }

            return result;
        }

        var errorResult = MessageBirdSmsResponseMapper.FromErrorResponse(
            Name,
            (int)response.StatusCode,
            responseBody);
        _logger.LogWarning(
            "MessageBird: send failed HTTP {StatusCode}, provider='{Provider}', error={Error}",
            (int)response.StatusCode,
            Name,
            errorResult.ErrorMessage);
        return errorResult;
    }
}
