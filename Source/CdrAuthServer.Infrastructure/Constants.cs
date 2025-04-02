namespace CdrAuthServer.Infrastructure
{
    public static class Constants
    {
        public static class Infosec
        {
            public const string CODE_CHALLENGE_METHOD = "S256";
        }

        public static class Headers
        {
            public const string X_V = "x-v";
            public const string X_MIN_V = "x-min-v";
            public const string X_TLS_CLIENT_CERT_THUMBPRINT = "X-TlsClientCertThumbprint";
            public const string X_TLS_CLIENT_CERT_COMMON_NAME = "X-TlsClientCertCN";
        }

        public static class Scopes
        {
            public const string OpenId = "openid";
            public const string Profile = "profile";
            public const string Registration = "cdr:registration"; // CDR_DYNAMIC_CLIENT_REGISTRATION
            public const string Common = "common:customer.basic:read common:customer.detail:read";
            public const string Banking = "bank:accounts.basic:read bank:accounts.detail:read bank:transactions:read bank:payees:read bank:regular_payments:read";
            public const string Energy = "energy:electricity.servicepoints.basic:read energy:electricity.servicepoints.detail:read energy:electricity.usage:read energy:electricity.der:read energy:accounts.basic:read energy:accounts.detail:read energy:accounts.paymentschedule:read energy:accounts.concessions:read energy:billing:read";
            public const string AdminMetadataUpdate = "admin:metadata:update"; // CDR_AUTHSERVER
            public const string AdminMetricsRead = "admin:metrics.basic:read";
            public const string BankingSectorScopes = $"{OpenId} {Profile} {Registration} {Common} {Banking}";
            public const string AllSectorScopes = $"{BankingSectorScopes} {Energy}";

            public static class ResourceApis
            {
                public static class Common
                {
                    public const string CustomerBasicRead = "common:customer.basic:read";
                }

                public static class Banking
                {
                    public const string AccountsBasicRead = "bank:accounts.basic:read";
                }
            }
        }

        public static class ConfigurationKeys
        {
            public const string EnableSwagger = "EnableSwagger";
        }

        public static class Versioning
        {
            public const string GroupNameFormat = "'v'VVV";
        }
    }
}
