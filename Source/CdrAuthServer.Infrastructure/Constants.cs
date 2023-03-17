using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CdrAuthServer.Infrastructure
{
    public static class Constants
    {
        public static class Infosec
        {
            public const string CODE_CHALLENGE_METHOD = "S256";
        }

        public static class Scopes
        {
            public const string CDR_DYNAMIC_CLIENT_REGISTRATION = "cdr:registration";
            public const string CDR_REGISTER = "cdr-register:read";
            public const string CDR_REGISTER_BANKING = "cdr-register:bank:read";

            public const string CDR_AUTHSERVER = "admin:metadata:update";
        }

        public static class GrantTypes
        {
            public const string CLIENT_CREDENTIALS = "client_credentials";
            public const string AUTH_CODE = "authorization_code";
        }
    }
}
