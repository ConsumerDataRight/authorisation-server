using System.Security;

namespace CdrAuthServer.Infrastructure.Extensions
{
    public class NoHttpsException : SecurityException
    {
        public NoHttpsException()
            : base("A non-https endpoint has been encountered and blocked")
        {
        }
    }
}
