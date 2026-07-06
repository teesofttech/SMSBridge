using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SmsBridge.Options;

namespace SmsBridge.Providers.AwsSns;

internal static class AwsSnsRequestSigner
{
    private const string Algorithm = "AWS4-HMAC-SHA256";
    private const string Service = "sns";

    public static void Sign(
        HttpRequestMessage request,
        AwsSnsOptions options,
        DateTimeOffset now,
        string payload)
    {
        if (request.RequestUri is null)
            throw new InvalidOperationException("Cannot sign a request without a request URI.");

        var amzDate = now.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var dateStamp = now.UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var host = request.RequestUri.Host;
        var credentialScope = $"{dateStamp}/{options.Region}/{Service}/aws4_request";
        const string signedHeaders = "host;x-amz-date";

        request.Headers.Host = host;
        request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);

        var canonicalRequest = string.Join(
            "\n",
            request.Method.Method,
            string.IsNullOrEmpty(request.RequestUri.AbsolutePath) ? "/" : request.RequestUri.AbsolutePath,
            string.Empty,
            $"host:{host}\n" +
            $"x-amz-date:{amzDate}\n",
            signedHeaders,
            Sha256Hex(payload));

        var stringToSign = string.Join(
            "\n",
            Algorithm,
            amzDate,
            credentialScope,
            Sha256Hex(canonicalRequest));

        var signingKey = GetSignatureKey(options.SecretAccessKey, dateStamp, options.Region, Service);
        var signature = ToHex(HmacSha256(signingKey, stringToSign));

        request.Headers.TryAddWithoutValidation(
            "Authorization",
            $"{Algorithm} Credential={options.AccessKeyId}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}");
    }

    private static byte[] GetSignatureKey(string key, string dateStamp, string regionName, string serviceName)
    {
        var kDate = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + key), dateStamp);
        var kRegion = HmacSha256(kDate, regionName);
        var kService = HmacSha256(kRegion, serviceName);
        return HmacSha256(kService, "aws4_request");
    }

    private static byte[] HmacSha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string Sha256Hex(string data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return ToHex(hash);
    }

    private static string ToHex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();
}
