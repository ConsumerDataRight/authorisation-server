using CdrAuthServer.Models;

namespace CdrAuthServer.Extensions
{
    public static class AuthorizationRequestObjectExtensions
    {
        public static bool IsHybridFlow(this AuthorizationRequestObject authorizationRequestObject)
        {
            if (authorizationRequestObject == null || string.IsNullOrEmpty(authorizationRequestObject.ResponseType))
            {
                return false;
            }

            return authorizationRequestObject.ResponseType.IsHybridFlow();
        }

        public static bool IsHybridFlow(this string responseType)
        {
            var responseTypeValues = responseType.Split(' ');
            return responseTypeValues.Contains("code") && responseTypeValues.Contains("id_token");
        }
    }
}
