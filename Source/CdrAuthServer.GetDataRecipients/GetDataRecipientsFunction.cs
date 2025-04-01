using CdrAuthServer.Infrastructure;
using CdrAuthServer.Infrastructure.Extensions;
using CdrAuthServer.Infrastructure.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CdrAuthServer.GetDataRecipients
{
    public class GetDataRecipientsFunction
    {
        private readonly ILogger _logger;
        private readonly GetDROptions _drOptions;

        public GetDataRecipientsFunction(ILoggerFactory loggerFactory, IOptions<GetDROptions> options)
        {
            _logger = loggerFactory.CreateLogger<GetDataRecipientsFunction>();
            _drOptions = options.Value;
        }

        /// <summary>
        /// Get Data Recipients Function.
        /// </summary>
        /// <remarks>Gets the Data Recipients from the Register and updates the local repository.</remarks>
        [Function("GetDataRecipients")]
        public async Task Run([TimerTrigger("%Schedule%")] TimerInfo myTimer)
        {
            var dbLoggingConnString = _drOptions.Register_CdrAuthServer_Logging_DB_ConnectionString;

            try
            {
                // Updated names for the cdrauth server
                var dataRecipientsEndpoint = _drOptions.Register_CdrAuthServer_MetadataUpdate_Endpoint;
                var ignoreServerCertificateErrors = _drOptions.Ignore_Server_Certificate_Errors.Equals("true", StringComparison.OrdinalIgnoreCase);

                // MTLS Auth server endpoints
                var authtokenEndpoint = _drOptions.Register_CdrAuthServer_Token_Endpoint;
                var clientCert = _drOptions.Register_Client_Certificate;
                var clientCertPwd = _drOptions.Register_Client_Certificate_Password;
                var signCert = _drOptions.Register_Signing_Certificate;
                var signCertPwd = _drOptions.Register_Signing_Certificate_Password;
                var clientId = _drOptions.Register_Client_Id;

                // Setup Get access token from the auth server endpoints,
                // Add token to the bear token call
                // Auth Api should receive the call and authorize it before calling on

                // Loading client certificates from Base64 string
                X509Certificate2 clientCertificate = LoadCertificates(_logger, clientCert, clientCertPwd);
                _logger.LogInformation("Client certificate loaded: {Thumbprint}", clientCertificate.Thumbprint);

                X509Certificate2 signCertificate = LoadCertificates(_logger, signCert, signCertPwd);
                _logger.LogInformation("Signing certificate loaded: {Thumbprint}", signCertificate.Thumbprint);

                Infrastructure.Models.Response<Token> tokenResponse = await GetAccessToken(authtokenEndpoint, clientId, clientCertificate, signCertificate, _logger, ignoreServerCertificateErrors);

                if (tokenResponse.IsSuccessful)
                {
                    // Send access token as bear token request to autherization server
                    (_, var respStatusCode) = await GetDataRecipients(dataRecipientsEndpoint, tokenResponse.Data.AccessToken, clientCertificate, _logger, ignoreServerCertificateErrors);
                    if (respStatusCode != System.Net.HttpStatusCode.OK)
                    {
                        await InsertDBLog(dbLoggingConnString, "Error", "GetDataRecipients", $"DR StatusCode: {respStatusCode}");
                    }
                }
                else
                {
                    await InsertDBLog(dbLoggingConnString, $"Unable to get the Access Token for {clientId}", "GetDataRecipients", $"StatusCode: {tokenResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                await InsertDBLog(dbLoggingConnString, "Error", "Exception", "DATARECIPIENTS", ex);
            }
        }

        /// <summary>
        /// Returns certificates.
        /// </summary>
        /// <param name="log">logger interface.</param>
        /// <param name="cert">certificate name.</param>
        /// <param name="certPwd">password for the certificate.</param>
        /// <returns>the loaded certificate.</returns>
        private static X509Certificate2 LoadCertificates(ILogger log, string cert, string certPwd)
        {
            log.LogInformation("Loading the certificate...");
            byte[] certBytes = Convert.FromBase64String(cert);
            X509Certificate2 certificate = new (certBytes, certPwd, X509KeyStorageFlags.MachineKeySet);
            return certificate;
        }

        /// <summary>
        /// Get Access Token.
        /// </summary>
        /// <returns>JWT.</returns>
        private async Task<Infrastructure.Models.Response<Token>> GetAccessToken(
            string tokenEndpoint,
            string clientId,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            ILogger log,
            bool ignoreServerCertificateErrors = false)
        {
            // Setup the http client.
            var client = GetHttpClient(clientCertificate, ignoreServerCertificateErrors: ignoreServerCertificateErrors);

            // Make the request to the token endpoint.
            log.LogInformation("Retrieving access_token from the AuthServer: {TokenEndpoint}", tokenEndpoint);

            try
            {
                var response = await client.SendPrivateKeyJwtRequest(
                tokenEndpoint,
                signingCertificate,
                clientId,
                clientId,
                scope: Constants.Scopes.AdminMetadataUpdate,
                grantType: Domain.Constants.GrantTypes.ClientCredentials);

                var body = await response.Content.ReadAsStringAsync();

                var tokenResponse = new Infrastructure.Models.Response<Token>()
                {
                    StatusCode = response.StatusCode,
                };

                if (response.IsSuccessStatusCode)
                {
                    log.LogInformation("AuthServer response: {StatusCode} - {Body}", tokenResponse.StatusCode, body);
                    tokenResponse.Data = JsonConvert.DeserializeObject<Token>(body);
                }
                else
                {
                    await InsertDBLog(_drOptions.Register_CdrAuthServer_Logging_DB_ConnectionString, $"Failed to get access token for client {clientId}- {body}", "Error", "SendPrivateKeyJwtRequest");
                    tokenResponse.Message = body;
                }

                return tokenResponse;
            }
            catch (Exception ex)
            {
                await InsertDBLog(_drOptions.Register_CdrAuthServer_Logging_DB_ConnectionString, "Error", "Exception", "GetAccessToken", ex);
                log.LogError(ex, "Caught exception in GetAccessToken");
            }

            return new Infrastructure.Models.Response<Token>()
            {
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
            };
        }

        /// <summary>
        /// Get the list of Data Recipients from the Register.
        /// </summary>
        /// <returns>Raw data.</returns>
        private async Task<(string, System.Net.HttpStatusCode)> GetDataRecipients(
            string dataRecipientsEndpoint,
            string accessToken,
            X509Certificate2 clientCertificate,
            ILogger log,
            bool ignoreServerCertificateErrors = false)
        {
            var client = GetHttpClient(
                clientCertificate: clientCertificate,
                accessToken: accessToken,
                ignoreServerCertificateErrors: ignoreServerCertificateErrors);

            log.LogInformation("Retrieving data recipients from the Register: {DataRecipientsEndpoint}", dataRecipientsEndpoint);
            var payload = "{\"data\": {\"action\": \"REFRESH\"}}";

            HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");

            var result = await client.PostAsync(dataRecipientsEndpoint, content);
            var data = await result.Content.ReadAsStringAsync();
            log.LogInformation("Register response: {StatusCode} - {Body}", result.StatusCode, data);

            return (data, result.StatusCode);
        }

        private HttpClient GetHttpClient(
            X509Certificate2 clientCertificate = null,
            string accessToken = null,
            bool ignoreServerCertificateErrors = false)
        {
            var clientHandler = new HttpClientHandler();

            // Set the client certificate for the connection if supplied.
            if (clientCertificate != null)
            {
                clientHandler.ClientCertificates.Add(clientCertificate);
            }

            if (ignoreServerCertificateErrors)
            {
                clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            }

            var client = new HttpClient(clientHandler);

            // If an access token has been provided then add to the Authorization header of the client.
            if (!string.IsNullOrEmpty(accessToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            // Add the x-v header to the request from configurations
            if (!string.IsNullOrEmpty(_drOptions.Register_CdrAuthServer_MetadataUpdate_XV))
            {
                client.DefaultRequestHeaders.Add("x-v", _drOptions.Register_CdrAuthServer_MetadataUpdate_XV);
            }

            // Add the x-min-v header to the request from configurations
            if (!string.IsNullOrEmpty(_drOptions.Register_CdrAuthServer_MetadataUpdate_XMINV))
            {
                client.DefaultRequestHeaders.Add("x-min-v", _drOptions.Register_CdrAuthServer_MetadataUpdate_XMINV);
            }

            return client;
        }

        /// <summary>
        /// Update the Log table.
        /// </summary>
        private static async Task InsertDBLog(string dbConnString, string msg, string lvl, string methodName, Exception exMsg = null, string entity = "")
        {
            string exMessage = string.Empty;

            if (exMsg != null)
            {
                Exception innerException = exMsg;
                StringBuilder innerMsg = new ();
                int ctr = 0;

                do
                {
                    // skip the first inner exeception message as it is the same as the exception message
                    if (ctr > 0)
                    {
                        innerMsg.Append(string.IsNullOrEmpty(innerException.Message) ? string.Empty : innerException.Message);
                        innerMsg.Append("\r\n");
                    }
                    else
                    {
                        ctr++;
                    }

                    innerException = innerException.InnerException;
                }
                while (innerException != null);

                // Use the Exception message
                if (innerMsg.Length == 0)
                {
                    exMessage = exMsg.Message;
                }

                // Use the inner Exception message (includes the Exception message)
                else
                {
                    exMessage = innerMsg.ToString();
                }

                // Include the serialised entity for use with Exception message only
                if (!string.IsNullOrEmpty(entity))
                {
                    exMessage += "\r\nEntity: " + entity;
                }

                exMessage = exMessage.Replace("'", string.Empty);
            }

            using SqlConnection db = new (dbConnString);
            await db.OpenAsync();
            var cmdText = string.Empty;

            if (string.IsNullOrEmpty(exMessage))
            {
                cmdText = $"INSERT INTO [LogEvents-DrService] ([Message], [Level], [TimeStamp], [ProcessName], [MethodName], [SourceContext]) VALUES (@msg,@lvl,GETUTCDATE(),@procName,@methodName,@srcContext)";
            }
            else
            {
                cmdText = $"INSERT INTO [LogEvents-DrService] ([Message], [Level], [TimeStamp], [Exception], [ProcessName], [MethodName], [SourceContext]) VALUES (@msg,@lvl,GETUTCDATE(), @exMessage,@procName,@methodName,@srcContext)";
            }

            using var cmd = new SqlCommand(cmdText, db);
            cmd.Parameters.AddWithValue("@msg", msg);
            cmd.Parameters.AddWithValue("@lvl", lvl);
            cmd.Parameters.AddWithValue("@exMessage", exMessage);
            cmd.Parameters.AddWithValue("@procName", "Azure Function");
            cmd.Parameters.AddWithValue("@methodName", methodName);
            cmd.Parameters.AddWithValue("@srcContext", "CdrAuthServer.GetDataRecipients");
            await cmd.ExecuteNonQueryAsync();
            await db.CloseAsync();
        }
    }
}
