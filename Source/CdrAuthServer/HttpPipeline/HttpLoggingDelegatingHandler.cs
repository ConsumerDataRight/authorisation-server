namespace CdrAuthServer.HttpPipeline
{
    /// <summary>
    /// Logs request/responses to third parties when added to <see cref="HttpClient"/> pipeline.
    /// </summary>
    /// <remarks>
    /// If the default logging level is Information or higher this will not log anything and will require the following environment variable to be set
    /// <c>Logging__LogLevel__CdrAuthServer__HttpPipeline=Debug</c>.
    /// </remarks>
    /// <param name="logger">The logger.</param>
    public class HttpLoggingDelegatingHandler(ILogger<HttpLoggingDelegatingHandler> logger) : DelegatingHandler
    {
        private const string RequestMessage = """
                Sending request:
                  Method: { Request.Method }
                  URI:  { Request.Uri }
                  ContentType: { Request.MediaType }
                  Headers: { Request.Headers }
                  Body: 
                    { Request.Body }
                """;

        private const string ResponseMessage = """
                Received response:
                  StatusCode: { Response.StatusCode }
                  Headers: { Response.Headers }
                  Body: 
                    { Response.Body }
                """;

        /// <summary>
        /// Send the request and log the request/response details.
        /// </summary>
        /// <param name="request">The request to send.</param>
        /// <param name="cancellationToken">The cancellation token to be forwarded to downstream calls.</param>
        /// <returns>The response.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage? response = null;

            using (logger.BeginScope("Calling {Method}{Uri}", request.Method, request.RequestUri))
            {
                await Log(request, cancellationToken);

                response = await base.SendAsync(request, cancellationToken);

                await Log(response, cancellationToken);
            }

            return response;
        }

        /// <summary>
        /// Log the response.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="cancellationToken">The cancellation token to be forwarded to downstream calls.</param>
        private async Task Log(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var content = await (response.Content?.ReadAsStringAsync(cancellationToken) ?? Task.FromResult(string.Empty));

            logger.LogInformation(ResponseMessage, response.StatusCode, response.Headers, content);
        }

        /// <summary>
        /// Log the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token to be forwarded to downstream calls.</param>
        private async Task Log(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = await (request.Content?.ReadAsStringAsync(cancellationToken) ?? Task.FromResult(string.Empty));

            logger.LogInformation(RequestMessage, request.Method, request.RequestUri, request.Content?.Headers.ContentType?.MediaType, request.Headers, content);
        }
    }
}
