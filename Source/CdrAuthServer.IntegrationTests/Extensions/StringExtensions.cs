// using IdentityModel;
using Jose;
using System;
using System.Security.Cryptography;
using System.Text;

namespace CdrAuthServer.IntegrationTests.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Convert string to int
        /// </summary>
        public static int ToInt(this string str)
        {
            return Convert.ToInt32(str);
        }

        public static string CreatePkceChallenge(this string codeVerifier)
        {
            using (var sha256 = SHA256.Create())
            {
                var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                return Base64Url.Encode(challengeBytes);
            }
        }
    }
}