using CdrAuthServer.Infrastructure;
using CdrAuthServer.Infrastructure.Extensions;
using CdrAuthServer.Infrastructure.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CdrAuthServer.GetDataRecipients
{
    public static class GetDataRecipientsFunction
    {
        /// <summary>
        /// Get Data Recipients Function
        /// </summary>
        /// <remarks>Gets the Data Recipients from the Register and updates the local repository</remarks>
        [FunctionName("GetDataRecipients")]
        public static async Task DATARECIPIENTS([TimerTrigger("%Schedule%")] TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            var dbLoggingConnString = Environment.GetEnvironmentVariable("Register_CdrAuthServer_Logging_DB_ConnectionString");

            try
            {
                var isLocalDev = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT").Equals("Development");
                var configBuilder = new ConfigurationBuilder().SetBasePath(context.FunctionAppDirectory);

                if (isLocalDev)
                {
                    configBuilder.AddJsonFile("local.settings.json", optional: false, reloadOnChange: true);
                }

                // Updated names for the cdrauth server
                string dataRecipientsEndpoint = Environment.GetEnvironmentVariable("Register_CdrAuthServer_MetadataUpdate_Endpoint");
                bool ignoreServerCertificateErrors = Environment.GetEnvironmentVariable("Ignore_Server_Certificate_Errors").Equals("true", StringComparison.OrdinalIgnoreCase);

                // MTLS Auth server endpoints
                string authtokenEndpoint = Environment.GetEnvironmentVariable("Register_CdrAuthServer_Token_Endpoint");                
                string clientCert = Environment.GetEnvironmentVariable("Register_Client_Certificate");
                string clientCertPwd = Environment.GetEnvironmentVariable("Register_Client_Certificate_Password");                
                string signCert = Environment.GetEnvironmentVariable("Register_Signing_Certificate");
                string signCertPwd = Environment.GetEnvironmentVariable("Register_Signing_Certificate_Password");                
                string clientId = Environment.GetEnvironmentVariable("Register_Client_Id");

                // Setup Get access token from the auth server endpoints,
                // Add token to the bear token call
                // Auth Api should receive the call and authorize it before calling on

                //Loading client certificates from Base64 string
                X509Certificate2 clientCertificate = LoadCertificates(log, clientCert, clientCertPwd);
                log.LogInformation("Client certificate loaded: {thumbprint}", clientCertificate.Thumbprint);

                X509Certificate2 signCertificate = LoadCertificates(log, signCert, signCertPwd);
                log.LogInformation("Signing certificate loaded: {thumbprint}", signCertificate.Thumbprint);

                Infrastructure.Models.Response<Token> tokenResponse = await GetAccessToken(authtokenEndpoint, clientId, clientCertificate, signCertificate, log, ignoreServerCertificateErrors);

                if (tokenResponse.IsSuccessful)
                {
                    // Send access token as bear token request to autherization server                    
                    (_, var respStatusCode) = await GetDataRecipients(dataRecipientsEndpoint, tokenResponse.Data.AccessToken, clientCertificate, log, ignoreServerCertificateErrors);
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
        /// Returns certificates
        /// </summary>
        /// <param name="log"></param>
        /// <param name="cert"></param>
        /// <param name="certPwd"></param>
        /// <returns></returns>
        private static X509Certificate2 LoadCertificates(ILogger log, string cert, string certPwd)
        {
            log.LogInformation("Loading the certificate...");
            byte[] certBytes = Convert.FromBase64String(cert);
            X509Certificate2 certificate = new(certBytes, certPwd, X509KeyStorageFlags.MachineKeySet);            
            return certificate;
        }

        /// <summary>
        /// Get Access Token
        /// </summary>
        /// <returns>JWT</returns>
        private static async Task<Infrastructure.Models.Response<Token>> GetAccessToken(
            string tokenEndpoint,
            string clientId,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            ILogger log,
            bool ignoreServerCertificateErrors = false)
        {
            var dbLoggingConnString = Environment.GetEnvironmentVariable("Register_CdrAuthServer_Logging_DB_ConnectionString");

            // Setup the http client.
            var client = GetHttpClient(clientCertificate, ignoreServerCertificateErrors: ignoreServerCertificateErrors);

            // Make the request to the token endpoint.
            log.LogInformation("Retrieving access_token from the AuthServer: {tokenEndpoint}", tokenEndpoint);

            try
            {
                var response = await client.SendPrivateKeyJwtRequest(
                tokenEndpoint,
                signingCertificate,
                clientId,
                clientId,
                scope: Constants.Scopes.CDR_AUTHSERVER,
                grantType: Constants.GrantTypes.CLIENT_CREDENTIALS);

                var body = await response.Content.ReadAsStringAsync();

                var tokenResponse = new Infrastructure.Models.Response<Token>()
                {
                    StatusCode = response.StatusCode
                };

                if (response.IsSuccessStatusCode)
                {
                    log.LogInformation("AuthServer response: {statusCode} - {body}", tokenResponse.StatusCode, body);
                    tokenResponse.Data = JsonConvert.DeserializeObject<Token>(body);
                }
                else
                {
                    await InsertDBLog(dbLoggingConnString, $"Failed to get access token for client {clientId}- {body}", "Error", "SendPrivateKeyJwtRequest");
                    tokenResponse.Message = body;
                }

                return tokenResponse;
            }
            catch(Exception ex)
            {
                await InsertDBLog(dbLoggingConnString, "Error", "Exception", "GetAccessToken", ex);
                log.LogError(ex, "Caught exception in GetAccessToken");
            }

            return new Infrastructure.Models.Response<Token>()
            {
                StatusCode = System.Net.HttpStatusCode.InternalServerError
            };
        }


        /// <summary>
        /// Get the list of Data Recipients from the Register
        /// </summary>
        /// <returns>Raw data</returns>
        private static async Task<(string, System.Net.HttpStatusCode)> GetDataRecipients(
            string dataRecipientsEndpoint,
            string accessToken,
            X509Certificate2 clientCertificate,            
            ILogger log,
            bool ignoreServerCertificateErrors = false)
        {
            var data = string.Empty;            
            var client = GetHttpClient(clientCertificate:clientCertificate,
                accessToken:accessToken,
                ignoreServerCertificateErrors: ignoreServerCertificateErrors);

            log.LogInformation("Retrieving data recipients from the Register: {dataRecipientsEndpoint}", dataRecipientsEndpoint);            
            var payload = "{\"data\": {\"action\": \"REFRESH\"}}";

            HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            
            var result = await client.PostAsync(dataRecipientsEndpoint, content);            
            data = result.Content.ReadAsStringAsync().Result;            
            log.LogInformation("Register response: {statusCode} - {body}", result.StatusCode, data);

            return (data, result.StatusCode);
        }

        private static HttpClient GetHttpClient(
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

            string xvVersion = Environment.GetEnvironmentVariable("Register_CdrAuthServer_MetadataUpdate_XV");
            string xminvVersion = Environment.GetEnvironmentVariable("Register_CdrAuthServer_MetadataUpdate_XMINV");

            if (ignoreServerCertificateErrors)
            {
                clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            }

            var client = new HttpClient(clientHandler);

            // If an access token has been provided then add to the Authorization header of the client.
            if (!string.IsNullOrEmpty(accessToken))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Add the x-v header to the request from configurations
            if (!string.IsNullOrEmpty(xvVersion))
            {
                client.DefaultRequestHeaders.Add("x-v", xvVersion);
            }

            // Add the x-min-v header to the request from configurations
            if (!string.IsNullOrEmpty(xminvVersion))
            {
                client.DefaultRequestHeaders.Add("x-min-v", xminvVersion);
            }

            return client;
        }
        
        /// <summary>
        /// Update the Log table
        /// </summary>
        private static async Task InsertDBLog(string dbConnString, string msg, string lvl, string methodName, Exception exMsg = null, string entity = "")
        {
            string exMessage = "";

            if (exMsg != null)
            {
                Exception innerException = exMsg;
                StringBuilder innerMsg = new();
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
                    exMessage = exMsg.Message;

                // Use the inner Exception message (includes the Exception message)
                else
                    exMessage = innerMsg.ToString();

                // Include the serialised entity for use with Exception message only
                if (!string.IsNullOrEmpty(entity))
                    exMessage += "\r\nEntity: " + entity;

                exMessage = exMessage.Replace("'", "");
            }

            using (SqlConnection db = new(dbConnString))
            {
                db.Open();
                var cmdText = "";

                if (string.IsNullOrEmpty(exMessage))
                    cmdText = $"INSERT INTO [LogEvents-DrService] ([Message], [Level], [TimeStamp], [ProcessName], [MethodName], [SourceContext]) VALUES (@msg,@lvl,GETUTCDATE(),@procName,@methodName,@srcContext)";
                else
                    cmdText = $"INSERT INTO [LogEvents-DrService] ([Message], [Level], [TimeStamp], [Exception], [ProcessName], [MethodName], [SourceContext]) VALUES (@msg,@lvl,GETUTCDATE(), @exMessage,@procName,@methodName,@srcContext)";

                using var cmd = new SqlCommand(cmdText, db);
                cmd.Parameters.AddWithValue("@msg", msg);
                cmd.Parameters.AddWithValue("@lvl", lvl);
                cmd.Parameters.AddWithValue("@exMessage", exMessage);
                cmd.Parameters.AddWithValue("@procName", "Azure Function");
                cmd.Parameters.AddWithValue("@methodName", methodName);
                cmd.Parameters.AddWithValue("@srcContext", "CdrAuthServer.GetDataRecipients");
                await cmd.ExecuteNonQueryAsync();
                db.Close();
            }
        }
    }
}