namespace CdrAuthServer.Exceptions
{
    public class JwksException : Exception
    {
        public JwksException()
            : base()
        {
        }

        public JwksException(string message)
            : base(message)
        {
        }
    }
}
