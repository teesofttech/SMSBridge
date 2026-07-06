using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.Providers.AwsSns;

internal sealed class AwsSnsSmsProvider : ISmsProvider
{
    private readonly AwsSnsOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AwsSnsSmsProvider> _logger;

    public string Name { get; }
    public SmsProviderType Type => SmsProviderType.AwsSns;

    public AwsSnsSmsProvider(
        string name,
        AwsSnsOptions options,
        IHttpClientFactory httpClientFactory,
        ILogger<AwsSnsSmsProvider> logger)
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
            "AWS SNS: sending SMS to {To} via provider '{Provider}'",
            message.To,
            Name);

        var url = $"https://sns.{_options.Region}.amazonaws.com/";
        var fields = AwsSnsSmsRequestMapper.ToFormFields(message, _options);
        using var content = new FormUrlEncodedContent(fields);
        var payload = await content.ReadAsStringAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payload)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        AwsSnsRequestSigner.Sign(request, _options, DateTimeOffset.UtcNow, payload);

        var client = _httpClientFactory.CreateClient(HttpClientNames.AwsSns);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "AWS SNS: HTTP request failed for provider '{Provider}'",
                Name);
            return SmsSendResult.Failed(
                Name,
                null,
                ex.Message,
                isTransient: true,
                mayHaveBeenAccepted: true);
        }

        using var responseLifetime = response;
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var result = AwsSnsSmsResponseMapper.FromResponse(Name, responseBody);
            if (result.Success)
            {
                _logger.LogInformation(
                    "AWS SNS: SMS accepted, id={MessageId}, provider='{Provider}'",
                    result.ProviderMessageId,
                    Name);
            }
            else
            {
                _logger.LogWarning(
                    "AWS SNS: provider returned an unparsable success response, provider='{Provider}', error={Error}",
                    Name,
                    result.ErrorMessage);
            }

            return result;
        }

        var errorResult = AwsSnsSmsResponseMapper.FromErrorResponse(
            Name,
            (int)response.StatusCode,
            responseBody);
        _logger.LogWarning(
            "AWS SNS: send failed HTTP {StatusCode}, provider='{Provider}', error={Error}",
            (int)response.StatusCode,
            Name,
            errorResult.ErrorMessage);
        return errorResult;
    }
}
