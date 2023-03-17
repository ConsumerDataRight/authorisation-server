using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace CdrAuthServer.IntegrationTests.Infrastructure.API2
{
    public static class DataHolder_Register_API
    {
        /// <summary>
        /// Create registration request JWT for SSA
        /// </summary>
        public static string CreateRegistrationRequest(
            string ssa,
            string token_endpoint_auth_signing_alg = "PS256",
            string[]? redirect_uris = null,
            string applicationType = "web",
            string requestObjectSigningAlg = "PS256",
            string jwtCertificateFilename = BaseTest.JWT_CERTIFICATE_FILENAME,
            string jwtCertificatePassword = BaseTest.JWT_CERTIFICATE_PASSWORD,
            string responseType = "code id_token",
            string[]? grant_types = null,
            string? authorization_signed_response_alg = null,
            string? authorization_encrypted_response_alg = null,
            string? authorization_encrypted_response_enc = null,

            string? idTokenSignedResponseAlg = "PS256",
            string? idTokenEncryptedResponseAlg = "RSA-OAEP",
            string? idTokenEncryptedResponseEnc = "A256GCM")
        {
            string[] responseTypes = responseType.Contains(",") ? responseType.Split(",") : new string[] { responseType };

            grant_types = grant_types ?? new string[] { "client_credentials", "authorization_code", "refresh_token" };

            var decodedSSA = new JwtSecurityTokenHandler().ReadJwtToken(ssa);

            var softwareId = decodedSSA.Claims.First(claim => claim.Type == "software_id").Value;

            var iat = (Int32)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
            var exp = iat + 300; // expire 5 mins from now

            var subject = new Dictionary<string, object?>
                {
                    { "iss", softwareId },
                    { "iat", iat },
                    { "exp", exp },
                    { "jti", Guid.NewGuid().ToString() },
                    { "aud", BaseTest.REGISTRATION_AUDIENCE_URI },
                    { "token_endpoint_auth_signing_alg", token_endpoint_auth_signing_alg },
                    { "token_endpoint_auth_method", "private_key_jwt" },
                    { "grant_types", grant_types },
                    { "response_types", responseTypes },
                    { "software_statement", ssa },
                };

            // Optional fields.
            if (!string.IsNullOrEmpty(applicationType))
            {
                subject.Add("application_type", applicationType);
            }

            if (!string.IsNullOrEmpty(requestObjectSigningAlg))
            {
                subject.Add("request_object_signing_alg", requestObjectSigningAlg);
            }

            if (redirect_uris != null && redirect_uris.Any())
            {
                subject.Add("redirect_uris", redirect_uris);
            }

            if (authorization_signed_response_alg != null)
            {
                if (authorization_signed_response_alg == BaseTest.NULL)
                {
                    subject.Add("authorization_signed_response_alg", null);
                }
                else
                {
                    subject.Add("authorization_signed_response_alg", authorization_signed_response_alg);
                }
            }

            if (authorization_encrypted_response_alg != null)
            {
                subject.Add("authorization_encrypted_response_alg", authorization_encrypted_response_alg);
            }
            if (authorization_encrypted_response_enc != null)
            {
                subject.Add("authorization_encrypted_response_enc", authorization_encrypted_response_enc);
            }

            if (idTokenSignedResponseAlg != null)
            {
                subject.Add("id_token_signed_response_alg", idTokenSignedResponseAlg);
            }
            if (idTokenEncryptedResponseAlg != null)
            {
                subject.Add("id_token_encrypted_response_alg", idTokenEncryptedResponseAlg);
            }
            if (idTokenEncryptedResponseEnc != null)
            {
                subject.Add("id_token_encrypted_response_enc", idTokenEncryptedResponseEnc);
            }

            var jwt = JWT2.CreateJWT2(
               jwtCertificateFilename,
               jwtCertificatePassword,
               subject);

            return jwt;
        }

        /// <summary>
        /// Register software product using registration request
        /// </summary>
        public static async Task<HttpResponseMessage> RegisterSoftwareProduct(string registrationRequest)
        {
            var url = $"{BaseTest.DH_MTLS_GATEWAY_URL}/connect/register";           

            // Post the request
            var api = new Infrastructure.API
            {
                URL = url,
                CertificateFilename = BaseTest.CERTIFICATE_FILENAME,
                CertificatePassword = BaseTest.CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                Content = new StringContent(registrationRequest, Encoding.UTF8, "application/jwt"),
                ContentType = MediaTypeHeaderValue.Parse("application/jwt"),
                Accept = "application/json"
            };
            var response = await api.SendAsync();

            return response;
        }
    }
}
