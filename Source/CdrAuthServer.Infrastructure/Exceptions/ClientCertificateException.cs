using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CdrAuthServer.Infrastructure.Exceptions
{
    [Serializable]
    public class ClientCertificateException : Exception
    {
        public ClientCertificateException(string message) : base(message)
        {
        }    
        protected ClientCertificateException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
