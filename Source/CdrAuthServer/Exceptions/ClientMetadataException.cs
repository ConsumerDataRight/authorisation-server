using System.Runtime.Serialization;

namespace CdrAuthServer.Exceptions
{
    public class ClientMetadataException : Exception
    {
        public ClientMetadataException(string field) : base($"Client metadata error: {field}")
        {
        }

        public ClientMetadataException(string field, string message) : base($"{field}: {message}")
        {
        }
    }
}
