using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using SmsBridge.Abstractions;
using SmsBridge.Options;

namespace SmsBridge.Providers.Esendex;

internal static class EsendexSmsRequestMapper
{
    internal static HttpRequestMessage CreateRequest(SmsMessage message, EsendexOptions options)
    {
        var from = string.IsNullOrWhiteSpace(message.From) ? options.From : message.From;
        var body = new XElement("messages",
            new XElement("accountreference", options.AccountReference),
            new XElement("message",
                string.IsNullOrWhiteSpace(from) ? null : new XElement("from", from),
                new XElement("to", message.To),
                new XElement("body", message.Body),
                new XElement("characterset", "Auto")));

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}"));
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.esendex.com/v1.0/messagedispatcher")
        {
            Content = new StringContent(body.ToString(SaveOptions.DisableFormatting), Encoding.UTF8, "application/xml")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        return request;
    }
}
