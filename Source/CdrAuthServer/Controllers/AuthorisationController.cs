using CdrAuthServer.Configuration;
using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using CdrAuthServer.Validation;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Web;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Controllers
{
    [ApiController]
    public class AuthorisationController : ControllerBase
    {
        private readonly ILogger<AuthorisationController> _logger;
        private readonly IConfiguration _config;
        private readonly IAuthorizeRequestValidator _authorizeRequestValidator;
        private readonly IClientService _clientService;
        private readonly ITokenService _tokenService;
        private readonly IGrantService _grantService;

        public AuthorisationController(
            IConfiguration config,
            ILogger<AuthorisationController> logger,
            IAuthorizeRequestValidator authorizeRequestValidator,
            IGrantService grantService,
            IClientService clientService,
            ITokenService tokenService)
        {
            _logger = logger;
            _config = config;
            _authorizeRequestValidator = authorizeRequestValidator;
            _clientService = clientService;
            _grantService = grantService;
            _clientService = clientService;
            _tokenService = tokenService;
        }

        [HttpGet]
        [Route("/connect/authorize")]
        public async Task<IActionResult> Authorise(
            [FromQuery] AuthorizeRequest authRequest)
        {
            var configOptions = _config.GetConfigurationOptions(this.HttpContext);
            var validationResult = await _authorizeRequestValidator.Validate(authRequest, configOptions);
            if (!validationResult.IsValid)
            {
                _logger.LogInformation("Authorization failed {@validationResult}", validationResult);
                return await CallbackErrorResponse(validationResult, authRequest.client_id, configOptions);
            }

            // Retrieve the request uri grant, this has already been validated in the validator.
            if (await _grantService.Get(GrantTypes.RequestUri, authRequest.request_uri, authRequest.client_id) is not RequestUriGrant requestUriGrant)
            {
                _logger.LogError("requestUriGrant for request_uri:{uri} for client:{id} not found", authRequest.request_uri, authRequest.client_id);
                throw new InvalidOperationException($"requestUriGrant is null or not found");
            }
            requestUriGrant.UsedAt = DateTime.UtcNow;
            await _grantService.Update(requestUriGrant);

            if (configOptions.HeadlessMode)
            {
                if (configOptions.HeadlessModeRequiresConfirmation)
                {
                    return OutputAuthConfirmation($"{configOptions.BaseUri}{configOptions.BasePath}/connect/authorize-confirm", authRequest.request_uri, authRequest.client_id);
                }

                var user = new HeadlessModeUser();
                return await ProcessAuthResponse(validationResult.ValidatedAuthorizationRequestObject, user.Subject, user.Accounts, configOptions);
            }

            // Create params to redirect to the auth UI
            var client = await _clientService.Get(authRequest.client_id);

            //get sharing_duration
            var parRequestData = JsonConvert.DeserializeObject<AuthorizationRequestObject>(requestUriGrant.Data["request"] as string);

            var authorizeRedirectRequest = new AuthorizeRedirectRequest
            {
                ReturnUrl = $"{configOptions.BaseUri}/connect/authorize-callback",
                DhBrandName = configOptions.BrandName,
                DhBrandAbn = configOptions.BrandAbn,
                DrBrandName = client == null ? string.Empty : client.ClientName,
                CustomerId = configOptions.AutoFillCustomerId,
                Otp = configOptions.AutoFillOtp,
                AuthorizeRequest = authRequest,
                Scope = validationResult.ValidatedAuthorizationRequestObject.Scope,
                SharingDuration = parRequestData?.Claims.SharingDuration
            };
            var jwtToken = await _tokenService.CreateToken(
               new List<Claim>()
               {
                  new Claim("login_params", JsonConvert.SerializeObject(authorizeRedirectRequest))
               },
               client?.ClientId ?? string.Empty,
               TokenTypes.Jwt,
               300,
               configOptions);
            return Redirect($"{configOptions.AuthUiBaseUri}/ui/login?code={jwtToken}");
        }

        [HttpGet]
        [Route("/connect/authorize-callback")]
        public async Task<IActionResult> AuthoriseCallBack(
            [FromQuery] AuthorizeCallbackRequest authCallbackRequest)
        {
            var configOptions = _config.GetConfigurationOptions(this.HttpContext);
            var validationResult = await _authorizeRequestValidator.Validate(authCallbackRequest, configOptions, false);
            validationResult = _authorizeRequestValidator.ValidateCallback(validationResult, authCallbackRequest);
            if (!validationResult.IsValid)
            {
                _logger.LogInformation("Authorization failed {@validationResult}", validationResult);
                return await CallbackErrorResponse(validationResult, authCallbackRequest.client_id, configOptions);
            }

            // Retrieve the request uri grant, this has already been validated in the validator.
            if (await _grantService.Get(GrantTypes.RequestUri, authCallbackRequest.request_uri, authCallbackRequest.client_id) is not RequestUriGrant requestUriGrant)
            {
                _logger.LogError("requestUriGrant for request_uri:{uri} for client:{id} not found", authCallbackRequest.request_uri, authCallbackRequest.client_id);
                throw new InvalidOperationException($"requestUriGrant is null or not found");
            }

            return await ProcessAuthResponse(
                validationResult.ValidatedAuthorizationRequestObject,
                authCallbackRequest.subject_id,
                authCallbackRequest.account_ids.Split(','),
                configOptions);
        }

        [HttpPost]
        [Route("/connect/authorize-confirm")]
        public async Task<IActionResult> AuthoriseConfirm(
           [FromForm] string clientId,
           [FromForm] string requestUri,
           [FromForm] string proceed,
           [FromForm] string cancel)
        {
            var configOptions = _config.GetConfigurationOptions(this.HttpContext);

            // This endpoint should only be called in HeadlessMode.
            if (!configOptions.HeadlessMode)
            {
                throw new InvalidOperationException("This endpoint is only available in Headless mode");
            }

            var authRequest = new AuthorizeRequest()
            {
                client_id = clientId,
                request_uri = requestUri,
            };
            var validationResult = await _authorizeRequestValidator.Validate(authRequest, configOptions, false);
            if (!validationResult.IsValid)
            {
                _logger.LogInformation("Authorization failed {@validationResult}", validationResult);
                return await CallbackErrorResponse(validationResult, authRequest.client_id, configOptions);
            }

            // Retrieve the request uri grant, this has already been validated in the validator.
            var grant = await _grantService.Get(GrantTypes.RequestUri, requestUri, clientId) as RequestUriGrant;
            if (grant == null)
            {
                _logger.LogError("requestUriGrant for request_uri:{uri} for client:{id} not found", requestUri, clientId);
                throw new InvalidOperationException($"requestUriGrant is null or not found");
            }

            // If cancelling the process.
            if (!string.IsNullOrEmpty(cancel))
            {
                var error = ErrorCatalogue.Catalogue().GetErrorDefinition(ErrorCatalogue.ACCESS_DENIED);
                var result = new AuthorizeRequestValidationResult(false)
                {
                    Error = error.Error,
                    ErrorDescription = error.ErrorDescription,
                    ValidatedAuthorizationRequestObject = validationResult.ValidatedAuthorizationRequestObject,
                    StatusCode = error.StatusCode,
                };
                return await CallbackErrorResponse(result, clientId, configOptions);
            }

            // Complete the auth process.
            var user = new HeadlessModeUser();
            return await ProcessAuthResponse(
                validationResult.ValidatedAuthorizationRequestObject,
                user.Subject,
                user.Accounts,
                configOptions);
        }

        private async Task<IActionResult> ProcessAuthResponse(
            AuthorizationRequestObject authRequestObject,
            string subjectId,
            IList<string> accountIds,
            ConfigurationOptions configOptions)
        {
            var grant = new AuthorizationCodeGrant()
            {
                ClientId = authRequestObject.ClientId,
                Request = JsonConvert.SerializeObject(authRequestObject),
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                CreatedAt = DateTime.UtcNow,
                GrantType = GrantTypes.AuthCode,
                Key = Guid.NewGuid().ToString(),
                Scope = FilterScopes(authRequestObject.Scope, configOptions),
                SubjectId = subjectId,
                AccountIdDelimitedList = String.Join(',', accountIds)
            };
            await _grantService.Create(grant);

            _logger.LogInformation("created AuthorizationCodeGrant for client:{id} subjectid:{subid}", authRequestObject.ClientId, subjectId);
            string? idToken = null;

            // If using hybrid flow then we need to generate an id_token to return from authorisation endpoint.
            if (authRequestObject.ResponseType == ResponseTypes.Hybrid)
            {
                idToken = await _tokenService.IssueIdToken(
                    authRequestObject.ClientId,
                    subjectId,
                    configOptions,
                    true,
                    state: authRequestObject.State,
                    nonce: authRequestObject.Nonce,
                    authCode: grant.Key);
                _logger.LogInformation("created Id token for client:{id} subjectid:{subid}", authRequestObject.ClientId, subjectId);
            }

            var client = await _clientService.Get(authRequestObject.ClientId);
            return await CallbackResponse(authRequestObject, client, configOptions, grant, idToken);
        }

        private string FilterScopes(string scope, ConfigurationOptions configOptions)
        {
            var scopes = scope.Split(' ');
            return string.Join(" ", scopes.Where(
                s => configOptions.ScopesSupported.Contains(s) && !configOptions.ClientCredentialScopesSupported.Contains(s)));
        }

        private async Task<IActionResult> CallbackResponse(
            AuthorizationRequestObject requestObject,
            Client client,
            ConfigurationOptions configOptions,
            AuthorizationCodeGrant grant,
            string? idToken = null)
        {
            // Send the error back to the client using query mode.
            if (requestObject.ResponseMode.StartsWith("query"))
            {
                var queryUri = await BuildQueryUri(requestObject, client, configOptions, grant.Key, idToken);
                return Redirect(queryUri);
            }

            // Send the error back to the client using form_post mode.
            if (requestObject.ResponseMode.StartsWith("form_post"))
            {
                return await FormPostCallback(requestObject, client, configOptions, grant.Key, idToken);
            }

            // Send the error back to the client using fragment mode.
            var fragmentUri = await BuildFragmentUri(requestObject, client, configOptions, grant.Key, idToken);
            return Redirect(fragmentUri);
        }

        private async Task<IActionResult> CallbackErrorResponse(
            AuthorizeRequestValidationResult result,
            string clientId,
            ConfigurationOptions configOptions)
        {
            // If the Redirect URI is not known, then we can only show the error on the page.
            if (string.IsNullOrEmpty(result.ValidatedAuthorizationRequestObject.RedirectUri))
            {
                return ShowError(result);
            }

            // Get the client so the registration metadata can be used.
            var client = await _clientService.Get(clientId);

            // Send the error back to the client using query mode.
            if (result.ValidatedAuthorizationRequestObject.ResponseMode.StartsWith("query"))
            {
                var queryUri = await BuildQueryUri(result.ValidatedAuthorizationRequestObject, client, configOptions, error: result.Error, errorDescription: result.ErrorDescription);
                return Redirect(queryUri);
            }

            // Send the error back to the client using form_post mode.
            if (result.ValidatedAuthorizationRequestObject.ResponseMode.StartsWith("form_post"))
            {
                return await FormPostCallback(result.ValidatedAuthorizationRequestObject, client, configOptions, error: result.Error, errorDescription: result.ErrorDescription);
            }

            // Send the error back to the client using fragment mode.
            var fragmentUri = await BuildFragmentUri(result.ValidatedAuthorizationRequestObject, client, configOptions, error: result.Error, errorDescription: result.ErrorDescription);
            return Redirect(fragmentUri);
        }

        private async Task<string> BuildFragmentUri(
            AuthorizationRequestObject requestObject,
            Client client,
            ConfigurationOptions configOptions,
            string? authCode = null,
            string? idToken = null,
            string? error = null,
            string? errorDescription = null)
        {
            return $"{requestObject.RedirectUri}#{await BuildQueryString(requestObject, client, configOptions, authCode, idToken, error, errorDescription)}";
        }

        private async Task<string> BuildQueryUri(
            AuthorizationRequestObject requestObject,
            Client client,
            ConfigurationOptions configOptions,
            string? authCode = null,
            string? idToken = null,
            string? error = null,
            string? errorDescription = null)
        {
            var delimiter = '?';
            if (requestObject.RedirectUri.Contains('?'))
            {
                delimiter = '&';
            }
            return $"{requestObject.RedirectUri}{delimiter}{await BuildQueryString(requestObject, client, configOptions, authCode, idToken, error, errorDescription)}";
        }

        private async Task<IActionResult> FormPostCallback(
            AuthorizationRequestObject requestObject,
            Client client,
            ConfigurationOptions configOptions,
            string? authCode = null,
            string? idToken = null,
            string? error = null,
            string? errorDescription = null)
        {
            var html = new StringBuilder();
            html.AppendLine("<!doctype html>");
            html.AppendLine("<html>");
            html.AppendLine("<body>");

            // Build the form to post back to the callback.
            html.AppendLine($@"<form name=""form"" method=""post"" action=""{requestObject.RedirectUri}"">");

            // Add hidden fields that will be posted back to the callback.
            if (requestObject.ResponseMode.EndsWith("jwt"))
            {
                // JARM response.
                html.AppendLine($@"<input type=""hidden"" name=""response"" value=""{(await BuildJarmResponse(requestObject, client, configOptions, authCode, error, errorDescription))}"" />");
            }
            else
            {
                if (!string.IsNullOrEmpty(authCode))
                {
                    html.AppendLine($@"<input type=""hidden"" name=""code"" value=""{authCode}"" />");
                }

                if (!string.IsNullOrEmpty(idToken))
                {
                    html.AppendLine($@"<input type=""hidden"" name=""id_token"" value=""{idToken}"" />");
                }

                if (!string.IsNullOrEmpty(error))
                {
                    html.AppendLine($@"<input type=""hidden"" name=""error"" value=""{error}"" />");
                }

                if (!string.IsNullOrEmpty(errorDescription))
                {
                    html.AppendLine($@"<input type=""hidden"" name=""error_description"" value=""{errorDescription}"" />");
                }

                if (!string.IsNullOrEmpty(requestObject.State))
                {
                    html.AppendLine($@"<input type=""hidden"" name=""state"" value=""{requestObject.State}"" />");
                }
            }

            html.AppendLine(@"<noscript>Click here to finish the authorization process: <input type=""submit"" /></noscript>");
            html.AppendLine("</form>");
            html.AppendLine("<script>document.form.submit();</script>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            return Content(html.ToString(), "text/html");
        }

        private async Task<string> BuildQueryString(
            AuthorizationRequestObject requestObject,
            Client client,
            ConfigurationOptions configOptions,
            string? authCode = null,
            string? idToken = null,
            string? error = null,
            string? errorDescription = null)
        {
            // JARM response.
            if (requestObject.ResponseMode.EndsWith("jwt"))
            {
                return $"response={(await BuildJarmResponse(requestObject, client, configOptions, authCode, error, errorDescription))}";
            }

            var queryString = new StringBuilder();

            if (!string.IsNullOrEmpty(error))
            {
                queryString.Append($"&error={error}");
            }

            if (!string.IsNullOrEmpty(errorDescription))
            {
                queryString.Append($"&error_description={HttpUtility.UrlEncode(errorDescription)}");
            }

            if (!string.IsNullOrEmpty(authCode))
            {
                queryString.Append($"&code={authCode}");
            }

            if (!string.IsNullOrEmpty(idToken))
            {
                queryString.Append($"&id_token={idToken}");
            }

            if (!string.IsNullOrEmpty(requestObject.State))
            {
                queryString.Append($"&state={HttpUtility.UrlEncode(requestObject.State)}");
            }

            return queryString.ToString().TrimStart('&');
        }

        private async Task<string> BuildJarmResponse(
            AuthorizationRequestObject requestObject,
            Client client,
            ConfigurationOptions configOptions,
            string? authCode = null,
            string? error = null,
            string? errorDescription = null)
        {
            // Build the JWT for the JARM response.
            var claims = new List<Claim>();

            if (!string.IsNullOrEmpty(error))
            {
                claims.Add(new Claim(ClaimNames.Error, error));
            }

            if (!string.IsNullOrEmpty(errorDescription))
            {
                claims.Add(new Claim(ClaimNames.ErrorDescription, errorDescription));
            }

            if (!string.IsNullOrEmpty(authCode))
            {
                claims.Add(new Claim(ClaimNames.Code, authCode));
            }

            if (!string.IsNullOrEmpty(requestObject.State))
            {
                claims.Add(new Claim(ClaimNames.State, requestObject.State));
            }

            Microsoft.IdentityModel.Tokens.JsonWebKey? clientJwk = null;
            string? encryptedResponseAlg = null;
            string? encryptedResponseEnc = null;

            // If JARM encryption is required.
            if (configOptions.SupportJarmEncryption)
            {
                // Get the client enc jwk.
                var jwks = await _clientService.GetJwks(client);
                clientJwk = jwks.Keys.First(jwk => jwk.Alg == client.AuthorizationEncryptedResponseAlg);
                encryptedResponseAlg = client.AuthorizationEncryptedResponseAlg;
                encryptedResponseEnc = client.AuthorizationEncryptedResponseEnc;
            }

            return await _tokenService.CreateToken(
                claims,
                requestObject.ClientId,
                TokenTypes.Jwt,
                300,
                configOptions,
                signingAlg: client.AuthorizationSignedResponseAlg,
                encryptedResponseAlg: encryptedResponseAlg,
                encryptedResponseEnc: encryptedResponseEnc,
                clientJwk: clientJwk);
        }

        private IActionResult ShowError(AuthorizeRequestValidationResult result)
        {
            var html = new StringBuilder();
            html.AppendLine("<!doctype html>");
            html.AppendLine("<html>");
            html.AppendLine("<body>");
            html.AppendLine("<h1>Error</h1>");
            html.AppendLine("<dl>");
            html.AppendLine("<dt>Error:</dt>");
            html.AppendLine($"<dd>{result.Error}</dd>");
            html.AppendLine("<dt>Error description:</dt>");
            html.AppendLine($"<dd>{result.ErrorDescription}</dd>");
            html.AppendLine("</dl>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            return Content(html.ToString(), "text/html");
        }

        /// <summary>
        /// This is used to generate html that can be used to Confirm/Cancel an authorisation
        /// request when in HeadlessMode.
        /// </summary>
        /// <returns>IActionResult</returns>
        private IActionResult OutputAuthConfirmation(
            string confirmationUri,
            string requestUri,
            string clientId)
        {
            var html = new StringBuilder();
            html.AppendLine("<!doctype html>");
            html.AppendLine("<html>");
            html.AppendLine("<body>");
            html.AppendLine("<h1>Proceed with authorisation?</h1>");
            html.AppendLine("<div>");
            html.AppendLine($@"<form name=""form"" method=""post"" action=""{confirmationUri}"">");
            html.AppendLine($@"<input type=""hidden"" name=""requestUri"" value=""{requestUri}"" />");
            html.AppendLine($@"<input type=""hidden"" name=""clientId"" value=""{clientId}"" />");
            html.AppendLine($@"<input type=""submit"" name=""proceed"" value=""Proceed"" />");
            html.AppendLine($@"<input type=""submit"" name=""cancel"" value=""Cancel"" />");
            html.AppendLine("</form>");
            html.AppendLine("</div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            return Content(html.ToString(), "text/html");
        }
    }
}
