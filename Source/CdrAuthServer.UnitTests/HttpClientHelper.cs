using System;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CdrAuthServer.Extensions;
using Moq;
using Moq.Protected;

namespace CdrAuthServer.UnitTests
{
    /// <summary>
    /// Helper functionality for mocking HttpClient.
    /// </summary>
    public static class HttpClientHelper
    {
        /// <summary>
        /// Adds a mocked response for <see cref="HttpClientHandler.SendAsync(HttpRequestMessage, CancellationToken)"/> calls.
        /// </summary>
        /// <param name="clientHandler">The handler mock to decorate.</param>
        /// <param name="responseCode">The response code to return.</param>
        /// <param name="responseContent">The response content to return, if provided.</param>
        /// <param name="messageFilter">A message filtering predicate that can be used to apply the behaviour conditionally. For example, based on a route/method, headers, query params etc.</param>
        /// <remarks>
        ///     This can be used to configure a <see cref = "Mock{HttpClientHandler}" /> object that is passed when creating a <see cref="HttpClient"/> such as in the following example:
        ///    <code>
        ///      var handler = new Mock&lt;HttpClientHandler&gt;();
        ///      handler.AddMockedHttpResponse(HttpStatusCode.OK, new StringContent("Hello world"), m =&gt; m.RequestUri = new Uri("http://localhost/hello-world"));
        ///      var httpClient = new Mock&lt;HttpClient&gt;(handler.Object);
        ///    </code>
        /// </remarks>
        public static void AddMockedHttpResponse(this Mock<HttpClientHandler> clientHandler, HttpStatusCode responseCode, HttpContent? responseContent, Expression<Func<HttpRequestMessage, bool>>? messageFilter = null)
        {
            messageFilter ??= _ => true; // match anything by default

            clientHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is(messageFilter), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = responseCode,
                   Content = responseContent,
               });
        }

        /// <summary>
        /// Adds a mocked JSON response for <see cref="HttpClientHandler.SendAsync(HttpRequestMessage, CancellationToken)"/> calls.
        /// </summary>
        /// <remarks>
        ///     This can be used to configure a <see cref = "Mock{HttpClientHandler}" /> object that is passed when creating a <see cref="HttpClient"/> such as in the following example:
        ///    <code>
        ///      var handler = new Mock&lt;HttpClientHandler&gt;();
        ///      handler.AddMockedHttpResponseJson(HttpStatusCode.OK, new { Hello = "world"}, m =&gt; m.RequestUri = new Uri("http://localhost/hello-world"));
        ///      var httpClient = new Mock&lt;HttpClient&gt;(handler.Object);
        ///    </code>
        /// </remarks>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="clientHandler">The handler mock to decorate.</param>
        /// <param name="responseCode">The response code to return.</param>
        /// <param name="response">The object to that will be serialised as JSON and form the response content.</param>
        /// <param name="messageFilter">A message filtering predicate that can be used to apply the behaviour conditionally. For example, based on a route/method, headers, query params etc.</param>
        public static void AddMockedHttpResponseJson<T>(this Mock<HttpClientHandler> clientHandler, HttpStatusCode responseCode, T response, Expression<Func<HttpRequestMessage, bool>>? messageFilter = null)
        {
            var content = new StringContent(response!.ToJson(), new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"));
            AddMockedHttpResponse(clientHandler, responseCode, content, messageFilter);
        }

        /// <summary>
        /// Adds a mocked exception to be thrown on <see cref="HttpClientHandler.SendAsync(HttpRequestMessage, CancellationToken)"/> to emulate errors for negative testing.
        /// </summary>
        /// <remarks>
        ///     This can be used to configure a <see cref = "Mock{HttpClientHandler}" /> object that is passed when creating a <see cref="HttpClient"/> such as in the following example:
        ///    <code>
        ///      var handler = new Mock&lt;HttpClientHandler&gt;();
        ///      handler.AddMockedException&lt;NotImplementException&gt;(new("No hello"), m =&gt; m.RequestUri = new Uri("http://localhost/hello-world"));
        ///      var httpClient = new Mock&lt;HttpClient&gt;(handler.Object);
        ///    </code>
        /// </remarks>
        /// <typeparam name="TException">The type of the exception that will be returned.</typeparam>
        /// <param name="clientHandler">The handler mock to decorate.</param>
        /// <param name="exception">The exception to throw.</param>
        /// <param name="messageFilter">A message filtering predicate that can be used to apply the behaviour conditionally. For example, based on a route/method, headers, query params etc.</param>
        public static void AddMockedException<TException>(this Mock<HttpClientHandler> clientHandler, TException exception, Expression<Func<HttpRequestMessage, bool>>? messageFilter = null)
                where TException : Exception
        {
            messageFilter ??= _ => true; // match anything by default

            clientHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is(messageFilter), ItExpr.IsAny<CancellationToken>())
               .ThrowsAsync(exception);
        }
    }
}
