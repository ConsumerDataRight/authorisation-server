using System.Runtime.Serialization;

namespace CdrAuthServer.Exceptions
{
    [Serializable]
    public class JwksException : Exception
    {
        public JwksException() : base()
        {
        }

        public JwksException(string message) : base(message)
        {
        }
        
        protected JwksException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
