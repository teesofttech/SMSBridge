using System.Net.Http.Json;
using SmsBridge.Abstractions;
using SmsBridge.Options;

namespace SmsBridge.Providers.Arkesel;

internal static class ArkeselSmsRequestMapper
{
    internal static HttpRequestMessage CreateRequest(SmsMessage message, ArkeselOptions options)
    {
        var body = new Dictionary<string, object>
        {
            ["sender"] = message.From ?? options.Sender,
            ["message"] = message.Body,
            ["recipients"] = new[] { message.To.TrimStart('+') }
        };
        if (!string.IsNullOrWhiteSpace(options.UseCase))
            body["use_case"] = options.UseCase;

        var request = new HttpRequestMessage(HttpMethod.Post, "https://sms.arkesel.com/api/v2/sms/send");
        request.Headers.Add("api-key", options.ApiKey);
        request.Content = JsonContent.Create(body);
        return request;
    }
}
