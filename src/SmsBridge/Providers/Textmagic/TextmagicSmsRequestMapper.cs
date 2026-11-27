using System.Net.Http.Headers;
using System.Net.Http.Json;
using SmsBridge.Abstractions;
using SmsBridge.Options;

namespace SmsBridge.Providers.Textmagic;

internal static class TextmagicSmsRequestMapper
{
    internal static HttpRequestMessage CreateRequest(SmsMessage message, TextmagicOptions options)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://rest.textmagic.com/api/v2/messages");
        request.Headers.Add("X-TM-Username", options.Username);
        request.Headers.Add("X-TM-Key", options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = JsonContent.Create(new
        {
            text = message.Body,
            phones = message.To,
            from = message.From ?? options.From
        });
        return request;
    }
}
