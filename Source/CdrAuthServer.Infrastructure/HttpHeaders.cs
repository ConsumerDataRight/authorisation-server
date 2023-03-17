using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CdrAuthServer.Infrastructure
{
    public static class HttpHeaders
    {
        public const string ClientCertificateThumbprint = "X-TlsClientCertThumbprint";
        public const string ClientCertificateCommonName = "X-TlsClientCertCN";
        public const string ForwardedHost = "X-Forwarded-Host";
        public const string WWWAuthenticate = "WWW-Authenticate";
    }
}
