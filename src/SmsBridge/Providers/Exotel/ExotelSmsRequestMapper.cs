using System.Net.Http.Headers;
using System.Text;
using SmsBridge.Abstractions;
using SmsBridge.Options;

namespace SmsBridge.Providers.Exotel;

internal static class ExotelSmsRequestMapper
{
    internal static HttpRequestMessage CreateRequest(SmsMessage message, ExotelOptions options)
    {
        var host = options.Region switch
        {
            ExotelRegion.Singapore => "api.exotel.com",
            ExotelRegion.Mumbai => "api.in.exotel.com",
            _ => throw new ArgumentOutOfRangeException(nameof(options.Region))
        };
        var fields = new Dictionary<string, string>
        {
            ["From"] = string.IsNullOrWhiteSpace(message.From) ? options.From : message.From,
            ["To"] = message.To,
            ["Body"] = message.Body
        };
        if (!string.IsNullOrWhiteSpace(options.DltEntityId))
            fields["DltEntityId"] = options.DltEntityId;
        if (!string.IsNullOrWhiteSpace(options.DltTemplateId))
            fields["DltTemplateId"] = options.DltTemplateId;

        var accountSid = Uri.EscapeDataString(options.AccountSid);
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://{host}/v1/Accounts/{accountSid}/Sms/send.json")
        {
            Content = new FormUrlEncodedContent(fields)
        };
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.ApiKey}:{options.AuthToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }
}
