using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.UnitTests
{
    abstract public class BaseTest
    {
        public const string SOFTWARE_PRODUCT_ID_KEY = "CDRAuthServer:softwareProductId";
        public const string BRAND_ID_KEY = "CDRAuthServer:brandId";        
        public const string SOFTWAREPRODUCT_ID = "c6327f87-687a-4369-99a4-eaacd3bb8210";
        public const string JWT_CERTIFICATE_FILENAME = "Certificates/MDR/jwks.pfx";
        public const string JWT_CERTIFICATE_PASSWORD = "#M0ckDataRecipient#";

        public const string DH_MTLS_GATEWAY_URL = "https://localhost:8082";        
        public const string SOFTWARE_STATEMENT = @"eyJhbGciOiJQUzI1NiIsImtpZCI6IkY0RUEyOTlDNjA3OTQ3RTQ1OUFDNDdFNjlGNzI4OUYxNzRCNUI0REYiLCJ0eXAiOiJKV1QifQ.ewogICJsZWdhbF9lbnRpdHlfaWQiOiAiMThiNzVhNzYtNTgyMS00YzllLWI0NjUtNDcwOTI5MWNmMGY0IiwKICAibGVnYWxfZW50aXR5X25hbWUiOiAiU2FuZGJveCBEYXRhIFJlY2lwaWVudCIsCiAgImlzcyI6ICJjZHItcmVnaXN0ZXIiLAogICJpYXQiOiAxNjY2MDY1NTY4LAogICJleHAiOiAxNjY2MDY2MTY4LAogICJqdGkiOiAiODVjMTBmMTc5YWIzNGVmYzgxMWY1MTEwODQzNzIyYWIiLAogICJvcmdfaWQiOiAiZmZiMWM4YmEtMjc5ZS00NGQ4LTk2ZjAtMWJjMzRhNmI0MzZmIiwKICAib3JnX25hbWUiOiAiU01EUiIsCiAgImNsaWVudF9uYW1lIjogIlNhbmRib3ggRGF0YSBSZWNpcGllbnQgU29mdHdhcmUgUHJvZHVjdCIsCiAgImNsaWVudF9kZXNjcmlwdGlvbiI6ICJBIHByb2R1Y3QgdG8gaW50ZXJhY3Qgd2l0aCB0aGUgZWNvc3lzdGVtIiwKICAiY2xpZW50X3VyaSI6ICJodHRwczovL2RyLmRldi5jZHJzYW5kYm94Lmdvdi5hdSIsCiAgInJlZGlyZWN0X3VyaXMiOiBbCiAgICAiaHR0cHM6Ly9kci5kZXYuY2Ryc2FuZGJveC5nb3YuYXUvY29uc2VudC9jYWxsYmFjayIKICBdLAogICJsb2dvX3VyaSI6ICJodHRwczovL2NkcnNhbmRib3guZ292LmF1L2xvZ28xOTIucG5nIiwKICAidG9zX3VyaSI6ICJodHRwczovL2RyLmRldi5jZHJzYW5kYm94Lmdvdi5hdS90b3MiLAogICJwb2xpY3lfdXJpIjogImh0dHBzOi8vZHIuZGV2LmNkcnNhbmRib3guZ292LmF1L3BvbGljeSIsCiAgImp3a3NfdXJpIjogImh0dHBzOi8vZHIuZGV2LmNkcnNhbmRib3guZ292LmF1L2p3a3MiLAogICJyZXZvY2F0aW9uX3VyaSI6ICJodHRwczovL2RyLmRldi5jZHJzYW5kYm94Lmdvdi5hdS9yZXZvY2F0aW9uIiwKICAicmVjaXBpZW50X2Jhc2VfdXJpIjogImh0dHBzOi8vZHIuZGV2LmNkcnNhbmRib3guZ292LmF1IiwKICAic29mdHdhcmVfaWQiOiAiYzYzMjdmODctNjg3YS00MzY5LTk5YTQtZWFhY2QzYmI4MjEwIiwKICAic29mdHdhcmVfcm9sZXMiOiAiZGF0YS1yZWNpcGllbnQtc29mdHdhcmUtcHJvZHVjdCIsCiAgInNjb3BlIjogIm9wZW5pZCBwcm9maWxlIGNvbW1vbjpjdXN0b21lci5iYXNpYzpyZWFkIGNvbW1vbjpjdXN0b21lci5kZXRhaWw6cmVhZCBiYW5rOmFjY291bnRzLmJhc2ljOnJlYWQgYmFuazphY2NvdW50cy5kZXRhaWw6cmVhZCBiYW5rOnRyYW5zYWN0aW9uczpyZWFkIGJhbms6cmVndWxhcl9wYXltZW50czpyZWFkIGJhbms6cGF5ZWVzOnJlYWQgZW5lcmd5OmFjY291bnRzLmJhc2ljOnJlYWQgZW5lcmd5OmFjY291bnRzLmRldGFpbDpyZWFkIGVuZXJneTphY2NvdW50cy5jb25jZXNzaW9uczpyZWFkIGVuZXJneTphY2NvdW50cy5wYXltZW50c2NoZWR1bGU6cmVhZCBlbmVyZ3k6YmlsbGluZzpyZWFkIGVuZXJneTplbGVjdHJpY2l0eS5zZXJ2aWNlcG9pbnRzLmJhc2ljOnJlYWQgZW5lcmd5OmVsZWN0cmljaXR5LnNlcnZpY2Vwb2ludHMuZGV0YWlsOnJlYWQgZW5lcmd5OmVsZWN0cmljaXR5LmRlcjpyZWFkIGVuZXJneTplbGVjdHJpY2l0eS51c2FnZTpyZWFkIGNkcjpyZWdpc3RyYXRpb24iCn0.DJmLhaM8I8pINgqIHYbpsQ2a63upn_OcliFdWN0iIkJSBP1pmMA-EjZekFojTjlDcCN1boIwOwzYNOjKGRl2Sis10ViiDyJIWN27sxWcLud_y7sm7YMUNBX6aX-p5IvuZ0J3ZmJbjqY9RhcO_0CMIbrpmbbsAqPF3r4XqDwN__XFRBgp2NNL8VRpKEBNMbGl214qwUe_aKTJ3PRPjOTS72RX6OX8dLSxRB--PBHavBUpBnCsdSA2TzjtHLZxbq6VTLswhVExwN8WTrgw8xg1CHRFSxh4L4IJAwYYn0Tn2HmlVGFnaNGjaocxO_EXZrloiF45956CVV1hqmi0EgWWfw";
        public const string JWKS_URI = "https://localhost:7000/cdr-register/v1/jwks";
        
        /// <summary>        
        /// </summary>
        /// <param name="requireSS">Add SoftareStatement</param>
        /// <returns></returns>
        public static string GetJwtToken(bool requireSS = false)
        {
            string ISSUER = SOFTWAREPRODUCT_ID.ToLower();

            var now = DateTime.UtcNow;

            var additionalClaims = new List<Claim>
                {
                     new Claim("sub", ISSUER),
                     new Claim("iat", new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer)
                };

            if (requireSS)
            {
                additionalClaims.Add(new Claim("software_statement", SOFTWARE_STATEMENT));
            }

            var expires = now.AddMinutes(10);
            string? aud = null;

            aud = $"{DH_MTLS_GATEWAY_URL}/connect/token";

            var certificate = new X509Certificate2(JWT_CERTIFICATE_FILENAME, JWT_CERTIFICATE_PASSWORD, X509KeyStorageFlags.Exportable);
            var x509SigningCredentials = new X509SigningCredentials(certificate, SecurityAlgorithms.RsaSsaPssSha256);
            var jwt = new JwtSecurityToken(
                ISSUER,
                aud,
                additionalClaims,
                expires: expires,
                signingCredentials: x509SigningCredentials);

            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var jwtSecturtyToken = jwtSecurityTokenHandler.WriteToken(jwt);

            return jwtSecturtyToken;
        }

        public static JwtSecurityToken GetJwt(string client_id = "", string RedirectUriValue = "", bool isNbf = false)
        {
            string ISSUER = SOFTWAREPRODUCT_ID.ToLower();

            var now = DateTime.UtcNow;

            var additionalClaims = new List<Claim>
                {
                     new Claim("sub", ISSUER),
                     new Claim("iat", new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer)
                };

            if (!String.IsNullOrEmpty(client_id))
            {
                additionalClaims.Add(new Claim("client_id", client_id));
            }

            if (!String.IsNullOrEmpty(RedirectUriValue))
            {
                additionalClaims.Add(new Claim(ClaimNames.RedirectUri, RedirectUriValue));
            }
            
            if (isNbf)
            {
                var nbfClaim = new Claim(ClaimNames.NotBefore, new DateTimeOffset(DateTime.Now.AddMinutes(5)).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer);
                additionalClaims.Add(nbfClaim);
            }


            var expires = now.AddMinutes(10);
            string? aud = null;

            aud = $"{DH_MTLS_GATEWAY_URL}/connect/token";

            var certificate = new X509Certificate2(JWT_CERTIFICATE_FILENAME, JWT_CERTIFICATE_PASSWORD, X509KeyStorageFlags.Exportable);
            var x509SigningCredentials = new X509SigningCredentials(certificate, SecurityAlgorithms.RsaSsaPssSha256);
            var jwt = new JwtSecurityToken(
                ISSUER,
                aud,
                additionalClaims,
                expires: expires,
                signingCredentials: x509SigningCredentials);
            
            return jwt;
        }        
    }
}
