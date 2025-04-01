using static CdrAuthServer.Infrastructure.Constants;

namespace CdrAuthServer.Infrastructure.Authorisation
{
    public enum AuthServerAuthorisationPolicyAttribute
    {
        [AuthorisationPolicy("Registration", Scopes.Registration, false, true, false)]
        Registration,
        [AuthorisationPolicy("UserInfo", scopeRequirement: null, false, true, false)]
        UserInfo,
        [AuthorisationPolicy("GetCustomerBasic", Scopes.ResourceApis.Common.CustomerBasicRead, false, true, true)]
        GetCustomerBasic,
        [AuthorisationPolicy("GetBankingAccounts", Scopes.ResourceApis.Banking.AccountsBasicRead, false, true, true)]
        GetBankingAccounts,
        [AuthorisationPolicy("AdminMetadataUpdate", Scopes.AdminMetadataUpdate, false, true, false)]
        AdminMetadataUpdate,
    }
}
