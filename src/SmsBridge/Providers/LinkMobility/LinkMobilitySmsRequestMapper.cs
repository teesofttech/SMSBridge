using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using SmsBridge.Abstractions;
using SmsBridge.Options;

namespace SmsBridge.Providers.LinkMobility;

internal static class LinkMobilitySmsRequestMapper
{
    internal static HttpRequestMessage CreateRequest(SmsMessage message, LinkMobilityOptions options)
    {
        var source = string.IsNullOrWhiteSpace(message.From) ? options.From : message.From;
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(options.BaseUrl), "/sms/send"))
        {
            Content = JsonContent.Create(new
            {
                source,
                sourceTON = source.StartsWith('+') ? "MSISDN" : "ALPHANUMERIC",
                destination = message.To,
                userData = message.Body,
                platformId = options.PlatformId,
                platformPartnerId = options.PlatformPartnerId,
                useDeliveryReport = false,
                ignoreResponse = false
            })
        };
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }
}
