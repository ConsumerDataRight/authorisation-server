using System;
using System.Runtime.Serialization;

namespace CdrAuthServer.mTLS.Gateway.Certificates
{
    [Serializable]
    public class ClientCertificateException : Exception
    {
        public ClientCertificateException(string message) : base($"An error occurred validating the client certificate: {message}")
        {
        }

        public ClientCertificateException(string message, Exception ex) : base($"An error occurred validating the client certificate: {message}", ex)
        {
        }

        protected ClientCertificateException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

}
