using CdrAuthServer.Domain.Extensions;
using System.Security.Cryptography;
using System.Text;

namespace CdrAuthServer.Extensions
{
    public static class HashExtensions
    {
        /// <summary>
        /// Creates a SHA256 hash of the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>A hash</returns>
        public static string Sha256(this string input)
        {
            if (input.IsNullOrEmpty()) return string.Empty;
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = SHA256.HashData(bytes);

            return Convert.ToBase64String(hash);
        }
    }
}
