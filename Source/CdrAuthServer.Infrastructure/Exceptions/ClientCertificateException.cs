namespace CdrAuthServer.Infrastructure.Exceptions
{
    public class ClientCertificateException : Exception
    {
        public ClientCertificateException(string message)
            : base(message)
        {
        }
    }
}
