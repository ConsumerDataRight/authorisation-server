using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.X509;
using System.Collections;


namespace CdrAuthServer.Infrastructure.Certificates
{
    /// <summary>
    /// Online Certificate Status Protocol (OCSP) request builder class that is used to send a request
    /// to the defined OCSP responder defined in the certificates Authority Information Access (AIA) extension. 
    /// </summary>
    public class OcspRequester
    {
        private const string BEGIN_CERTIFICATE_MARKER = "-----BEGIN CERTIFICATE-----";
        private const string END_CERTIFICATE_MARKER = "-----END CERTIFICATE-----";

        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly string _ocspResponderUrl;
        private readonly byte[] _caCertBytes;

        public enum OcspResult
        {
            Unknown = 0,
            Good = 1,
            Revoked = 2,
            Error = 3,
        }

        public OcspRequester(string ocspResponderUrl, string caCertPem, ILogger logger, HttpClient httpClient)
        {
            _ocspResponderUrl = ocspResponderUrl;
            _caCertBytes = GetBytesFromPEM(caCertPem);
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<OcspResult> GetResult(string serialNumber)
        {
            var intCaFile = new X509Certificate(_caCertBytes);
            var ocspRequest = BuildOcspRequest(ConvertSerialNumber(serialNumber), intCaFile);
            var url = $"{_ocspResponderUrl.TrimEnd('/')}/{Convert.ToBase64String(ocspRequest)}";

            _logger.LogInformation("OCSP Request: {url}", url);

            var response = await _httpClient.GetAsync(url);

            _logger.LogInformation("OCSP Response: {statusCode}", response.StatusCode);

            // Error response.
            if (!response.IsSuccessStatusCode)
            {
                return OcspResult.Error;
            }

            var ocspBytes = await response.Content.ReadAsByteArrayAsync();
            var ocspResp = new OcspResp(ocspBytes);
            var ocspResponse = ocspResp.GetResponseObject() as BasicOcspResp;

            if (ocspResponse == null || ocspResponse.Responses.Length != 1)
            {
                _logger.LogInformation("OCSP Response is Unknown");
                return OcspResult.Unknown;
            }

            var revokedStatus = ocspResponse.Responses[0].GetCertStatus() as RevokedStatus;

            _logger.LogInformation("OCSP Response revocation status = {revokedStatus}", revokedStatus?.RevocationReason);

            return (revokedStatus == null ? OcspResult.Good : OcspResult.Revoked);
        }

        private BigInteger ConvertSerialNumber(string serialNumber)
        {
            var serialBytes = Convert.FromHexString(serialNumber.PadLeft(32, '0'));
            return new BigInteger(serialBytes);
        }

        private static byte[] BuildOcspRequest(BigInteger serialNumber, X509Certificate cacert)
        {
            OcspReqGenerator ocspRequestGenerator = new();
            CertificateID certId = new(CertificateID.HashSha1, cacert, serialNumber);
            ocspRequestGenerator.AddRequest(certId);

            var derObjectIds = new List<DerObjectIdentifier>(); //Distinguished Encoding Rules (DER)
            var extensionValues = new Hashtable();

            Asn1OctetString asn1OctetString = new DerOctetString(BigInteger.ValueOf(DateTime.Now.Ticks).ToByteArray());
            var nonceExtension = new X509Extension(false, asn1OctetString);

            extensionValues.Add(OcspObjectIdentifiers.PkixOcspNonce, nonceExtension);
            ocspRequestGenerator.SetRequestExtensions(new X509Extensions(derObjectIds, extensionValues));
            var ocspRequest = ocspRequestGenerator.Generate();

            return ocspRequest.GetEncoded();
        }

        public static byte[] GetBytesFromPEM(string pem)
        {
            var base64 = pem
                .Replace(BEGIN_CERTIFICATE_MARKER, "")
                .Replace(END_CERTIFICATE_MARKER, "")
                .Replace(Environment.NewLine, "");

            return Convert.FromBase64String(base64);
        }
    }
}
