using CdrAuthServer.Exceptions;
using CdrAuthServer.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace CdrAuthServer.UnitTests.Services
{
    internal class JwksServiceTests
    {
        private readonly JsonWebKeySet _jwksCached;
        private readonly JsonWebKeySet _jwks;
        private readonly JsonWebKey jwk1 = JsonWebKeyConverter.ConvertFromRSASecurityKey(new RsaSecurityKey(RSA.Create()));
        private readonly JsonWebKey jwk2 = JsonWebKeyConverter.ConvertFromRSASecurityKey(new RsaSecurityKey(RSA.Create()));

        private readonly Uri _cachedJwksUri = new("https://localhost/cached");
        private readonly Uri _uncachedJwksUri = new("https://localhost/not-cached");

        private readonly Mock<ILogger<JwksService>> _loggerMock = new();
        private readonly Mock<HttpClientHandler> _httpClientHandlerMock = new();
        private readonly Mock<IMemoryCache> _cacheMock = new();
        private readonly IConfiguration _config;

        public JwksServiceTests()
        {
            _jwks = new JsonWebKeySet { Keys = { jwk1, jwk2 } };
            _jwksCached = new JsonWebKeySet { Keys = { jwk1 } };
            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { { "CacheExpiryMinutes", "5" } })
                .Build();
        }

        /// <summary>
        /// Asserts that <see cref="IJwksService.GetJwks(Uri)"/> hits the cache for cached JWKS.
        /// </summary>
        [Test]
        public async Task GetJwksWhenCachedReturnsFromCache()
        {
            var httpClient = new Mock<HttpClient>(_httpClientHandlerMock.Object);
            object? jwks = _jwksCached;
            object? nullSet = null;
            _cacheMock.Setup(x => x.TryGetValue(_cachedJwksUri.ToString(), out jwks)).Returns(true);
            _cacheMock.Setup(x => x.TryGetValue(_uncachedJwksUri.ToString(), out nullSet)).Returns(true);

            var service = new JwksService(_loggerMock.Object, _config, httpClient.Object, _cacheMock.Object);

            var result = await service.GetJwks(_cachedJwksUri);
            httpClient.Verify(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Never);

            Assert.IsNotNull(result);
            Assert.Greater(result!.Keys.Count, 0);
        }

        /// <summary>
        /// Asserts that <see cref="IJwksService.GetJwks(System.Uri)"/> refreshes for uncached JWKS.
        /// </summary>
        [Test]
        public async Task GetJwksWhenNotCachedFetchesThenCaches()
        {
            // Arrange
            object? jwks = _jwksCached;
            object? nullSet = null;
            _cacheMock.Setup(x => x.TryGetValue(_cachedJwksUri.ToString(), out jwks)).Returns(true);
            _cacheMock.Setup(x => x.TryGetValue(_uncachedJwksUri.ToString(), out nullSet)).Returns(true);
            _cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(new Mock<ICacheEntry>().Object);

            _httpClientHandlerMock.AddMockedHttpResponseJson(HttpStatusCode.OK, _jwks, msg => msg.RequestUri == _uncachedJwksUri);
            var httpClient = new Mock<HttpClient>(_httpClientHandlerMock.Object);
            var service = new JwksService(_loggerMock.Object, _config, httpClient.Object, _cacheMock.Object);

            // Act
            var result = await service.GetJwks(_uncachedJwksUri);

            // Assert
            httpClient.VerifyAll();
            Assert.IsNotNull(result);
            Assert.Greater(result!.Keys.Count, 0);
        }

        /// <summary>
        /// Asserts that <see cref="IJwksService.GetJwks(Uri)"/> throws an exception if the JWKS endpoint returns with an unsuccessful error code.
        /// </summary>
        /// <param name="status">The HTTP status code to return to indicate an error fetching the JWKS.</param>
        /// <param name="exceptionMessage">The expected message for the exception which is expected to be thrown.</param>
        [TestCase(HttpStatusCode.NotFound, "https://localhost/not-cached returned 404.")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "https://localhost/not-cached returned " + nameof(HttpStatusCode.ServiceUnavailable))]
        public void GetJwksThrowsExceptionForStatusCode(HttpStatusCode status, string exceptionMessage)
        {
            // Arrange
            object? jwks = _jwksCached;
            object? nullSet = null;
            _cacheMock.Setup(x => x.TryGetValue(_cachedJwksUri.ToString(), out jwks)).Returns(true);
            _cacheMock.Setup(x => x.TryGetValue(_uncachedJwksUri.ToString(), out nullSet)).Returns(true);
            _cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(new Mock<ICacheEntry>().Object);

            _httpClientHandlerMock.AddMockedHttpResponse(status, new StringContent("this is not the JWKS you are looking for"), msg => msg.RequestUri == _uncachedJwksUri);
            var httpClient = new Mock<HttpClient>(_httpClientHandlerMock.Object);
            var service = new JwksService(_loggerMock.Object, _config, httpClient.Object, _cacheMock.Object);

            // Act & Assert
            var result = Assert.ThrowsAsync<JwksException>(async () => await service.GetJwks(_uncachedJwksUri));
            Assert.True(result!.Message.StartsWith(exceptionMessage));
        }

        /// <summary>
        /// Asserts that <see cref="IJwksService.GetJwks(Uri)"/> throws an exception if the call to fetch JWKS from the endpoint failed with an exception.
        /// </summary>
        [Test]
        public void GetJwksWhenNotCachedThrowsExceptionForClientException()
        {
            // Arrange
            object? jwks = _jwksCached;
            object? nullSet = null;
            _cacheMock.Setup(x => x.TryGetValue(_cachedJwksUri.ToString(), out jwks)).Returns(true);
            _cacheMock.Setup(x => x.TryGetValue(_uncachedJwksUri.ToString(), out nullSet)).Returns(true);
            _cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(new Mock<ICacheEntry>().Object);

            var expectedException = new NotImplementedException("Emulate an exception thrown");
            _httpClientHandlerMock.AddMockedException(expectedException);
            var httpClient = new Mock<HttpClient>(_httpClientHandlerMock.Object);
            var service = new JwksService(_loggerMock.Object, _config, httpClient.Object, _cacheMock.Object);

            // Act & Assert
            var result = Assert.ThrowsAsync<JwksException>(async () => await service.GetJwks(_uncachedJwksUri));
            Assert.AreEqual("An error occurred retrieving JWKS from https://localhost/not-cached - Emulate an exception thrown", result!.Message);
        }

        /// <summary>
        /// Asserts that <see cref="IJwksService.GetJwks(Uri)"/> throws an exception if the JWKS payload is not well-formed.
        /// </summary>
        [Test]
        public void GetJwksWhenNotCachedThrowsExceptionForInvalidResponsePayload()
        {
            // Arrange
            object? jwks = _jwksCached;
            object? nullSet = null;
            _cacheMock.Setup(x => x.TryGetValue(_cachedJwksUri.ToString(), out jwks)).Returns(true);
            _cacheMock.Setup(x => x.TryGetValue(_uncachedJwksUri.ToString(), out nullSet)).Returns(true);
            _cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(new Mock<ICacheEntry>().Object);

            _httpClientHandlerMock.AddMockedHttpResponse(HttpStatusCode.OK, new StringContent("this is not the JWKS you are looking for"), msg => msg.RequestUri == _uncachedJwksUri);
            var httpClient = new Mock<HttpClient>(_httpClientHandlerMock.Object);
            var service = new JwksService(_loggerMock.Object, _config, httpClient.Object, _cacheMock.Object);

            // Act & Assert
            var result = Assert.ThrowsAsync<JwksException>(async () => await service.GetJwks(_uncachedJwksUri));
            Assert.AreEqual("No valid JWKS found from " + _uncachedJwksUri, result!.Message);
        }

        /// <summary>
        /// Asserts that <see cref="IJwksService.GetJwks(Uri, string)"/> hits the cache for cached JWKS.
        /// </summary>
        [Test]
        public async Task GetJwksByKidWhenCachedReturnsFromCache()
        {
            var httpClient = new Mock<HttpClient>(_httpClientHandlerMock.Object);
            object? jwks = _jwksCached;
            object? nullSet = null;
            _cacheMock.Setup(x => x.TryGetValue(_cachedJwksUri.ToString(), out jwks)).Returns(true);
            _cacheMock.Setup(x => x.TryGetValue(_uncachedJwksUri.ToString(), out nullSet)).Returns(true);

            var service = new JwksService(_loggerMock.Object, _config, httpClient.Object, _cacheMock.Object);

            var result = await service.GetJwks(_cachedJwksUri, _jwks.Keys[0].KeyId);
            httpClient.Verify(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Never);

            Assert.IsNotNull(result);
            Assert.Greater(result!.Keys.Count, 0);
        }

        /// <summary>
        /// Asserts that <see cref="IJwksService.GetJwks(Uri, string)"/> refreshes for uncached JWKS.
        /// </summary>
        [Test]
        public async Task GetJwksByKidWhenNotCachedFetchesThenCaches()
        {
            // Arrange
            object? jwks = _jwksCached;
            object? nullSet = null;
            _cacheMock.Setup(x => x.TryGetValue(_cachedJwksUri.ToString(), out jwks)).Returns(true);
            _cacheMock.Setup(x => x.TryGetValue(_uncachedJwksUri.ToString(), out nullSet)).Returns(false);
            _cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(new Mock<ICacheEntry>().Object);

            _httpClientHandlerMock.AddMockedHttpResponseJson(HttpStatusCode.OK, _jwks, msg => msg.RequestUri == _uncachedJwksUri);
            var httpClient = new Mock<HttpClient>(_httpClientHandlerMock.Object);
            var service = new JwksService(_loggerMock.Object, _config, httpClient.Object, _cacheMock.Object);

            // Act
            var result = await service.GetJwks(_uncachedJwksUri, _jwks.Keys[1].KeyId);

            // Assert
            httpClient.VerifyAll();
            Assert.IsNotNull(result);
            Assert.Greater(result!.Keys.Count, 0);
        }
    }
}
