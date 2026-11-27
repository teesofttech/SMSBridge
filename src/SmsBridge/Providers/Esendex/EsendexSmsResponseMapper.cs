using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using SmsBridge.Abstractions;
using SmsBridge.Internal;

namespace SmsBridge.Providers.Esendex;

internal static class EsendexSmsResponseMapper
{
    private static readonly XNamespace ApiNamespace = "http://api.esendex.com/ns/";

    internal static SmsSendResult FromResponse(string providerName, string body)
    {
        try
        {
            using var input = new StringReader(body);
            using var reader = XmlReader.Create(input, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            });
            var root = XDocument.Load(reader).Root;
            if (root?.Name == ApiNamespace + "messageheaders")
            {
                var ids = root.Elements(ApiNamespace + "messageheader")
                    .Select(header => (string?)header.Attribute("id"))
                    .ToArray();

                if (ids.Length > 0 && ids.All(id => Guid.TryParse(id, out var guid) && guid != Guid.Empty))
                    return SmsSendResult.Succeeded(providerName, ids.Length == 1 ? ids[0] : null);
            }
        }
        catch (XmlException)
        {
            // An unreadable acknowledgement can still follow an accepted send.
        }

        return ProviderResponseFailures.InvalidSuccessResponse(providerName, "Esendex");
    }

    internal static SmsSendResult FromHttpError(string providerName, int status)
    {
        var ambiguous = status == 408 || status >= 500;
        return SmsSendResult.Failed(providerName, status.ToString(CultureInfo.InvariantCulture),
            $"Esendex returned HTTP {status}.", isTransient: ambiguous || status == 429,
            mayHaveBeenAccepted: ambiguous);
    }
}
