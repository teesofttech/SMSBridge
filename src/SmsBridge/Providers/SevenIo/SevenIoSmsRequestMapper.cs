using System.Net.Http.Headers;
using SmsBridge.Abstractions;
using SmsBridge.Options;

namespace SmsBridge.Providers.SevenIo;

internal static class SevenIoSmsRequestMapper
{
    internal static HttpRequestMessage CreateRequest(SmsMessage message, SevenIoOptions options)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://gateway.seven.io/api/sms");
        request.Headers.Add("X-Api-Key", options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["to"] = message.To,
            ["from"] = message.From ?? options.From,
            ["text"] = message.Body
        });
        return request;
    }
}
