#undef DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON

using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net;
using Xunit;
using Xunit.DependencyInjection;

namespace CdrAuthServer.IntegrationTests
{
    public class US12962_CDRAuthServer_OIDC_JWKS : BaseTest
    {
        private readonly IApiServiceDirector _apiServiceDirector;

        public US12962_CDRAuthServer_OIDC_JWKS(
            IApiServiceDirector apiServiceDirector, ITestOutputHelperAccessor testOutputHelperAccessor, IConfiguration config)
            : base(testOutputHelperAccessor, config)
        {
            if (testOutputHelperAccessor is null)
            {
                throw new ArgumentNullException(nameof(testOutputHelperAccessor));
            }

            _apiServiceDirector = apiServiceDirector ?? throw new System.ArgumentNullException(nameof(apiServiceDirector));
        }

        private class AC01_Expected
        {
            public class Key
            {
                public string? kty { get; set; }

                public string? use { get; set; }

                public string? kid { get; set; }

                public string? e { get; set; }

                public string? n { get; set; }
            }

            public Key[]? Keys { get; set; }
        }

        [Fact]
        public async Task AC01_FromMDH_Get_ShouldRespondWith_200OK_OIDC_JWKS()
        {
            // Act
            var response = await _apiServiceDirector.BuildAuthServerJWKSAPI().SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                // Assert - Check content type
                Assertions.AssertHasContentTypeApplicationJson(response.Content);

                // Assert - Check JWKS
                var actualJson = await response.Content.ReadAsStringAsync();
                var actual = JsonConvert.DeserializeObject<AC01_Expected>(actualJson);
                actual.Keys.Should().NotBeNull();
                actual.Keys?.Length.Should().Be(2);
                actual.Keys?[0].kty.Should().Be("RSA");
                actual.Keys?[0].use.Should().Be("sig");
                actual.Keys?[0].kid.Should().Be("7C5716553E9B132EF325C49CA2079737196C03DB");
                actual.Keys?[0].e.Should().Be("AQAB");
                actual.Keys?[0].n.Should().Be("muidQL6h9QizbiZxZi3rpwNVDy7mXjtcl-C2rpI4JZzo0n2x-3KAHoCuuR7ZcX3b2DgfkI2IB9NsspdtZsAgKO0MYDROCn8TrIPKlvP4M8YwNQ1modLS9IfVqZU6Tp_mWpn89po7oZiTGq-qihv-xBUQwHM9FHplPP6DvA5Yl5UUHDdN2s9qnodjBI3SAyuVOY6s9X9iv-wDBYvI_981nEYA7Ndgm-QxW6qH0FgA8OC4yLE8e2QDEjL31JAXAJDcUTRTwiQL5jv_hd9Wze6_Oe19mcl1RKn1-z_96riylD3VrwqAR5KkmkyI35WBytAdUU1jpyT1D-RVxX-G3FHoUCgXPDSyvul9Djet65KZE1mkzZfCmo_2s44XcF_Mv4cBfayMdNkodu2EgTsBzgd7lmGszlDhEMZeLDELOIXdQRs5b6g7pt6YRRcGfDo6eRBuR6n9VCES5L9RNizUI--LISnM-W9tWxReGDoj6-YwLFq7bHNt42psvxJO96f3ISwn"); // MJS - This should be derived

#if DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON
                await WriteJsonToFileAsync($"c:/temp/actual.json", response.Content);
#endif
            }
        }

        [Fact]
        public async Task AC01_Get_ShouldRespondWith_200OK_OIDC_JWKS()
        {
            // Act
            var response = await _apiServiceDirector.BuildAuthServerJWKSAPI().SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                // Assert - Check content type
                Assertions.AssertHasContentTypeApplicationJson(response.Content);

                // Assert - Check json
                var expected = @"
                {
                    ""keys"": [
                        {
                        ""kty"": ""RSA"",
                        ""use"": ""sig"",
                        ""kid"": ""7C5716553E9B132EF325C49CA2079737196C03DB"",
                        ""x5t"": ""fFcWVT6bEy7zJcScogeXNxlsA9s"",
                        ""e"": ""AQAB"",
                        ""n"": ""muidQL6h9QizbiZxZi3rpwNVDy7mXjtcl-C2rpI4JZzo0n2x-3KAHoCuuR7ZcX3b2DgfkI2IB9NsspdtZsAgKO0MYDROCn8TrIPKlvP4M8YwNQ1modLS9IfVqZU6Tp_mWpn89po7oZiTGq-qihv-xBUQwHM9FHplPP6DvA5Yl5UUHDdN2s9qnodjBI3SAyuVOY6s9X9iv-wDBYvI_981nEYA7Ndgm-QxW6qH0FgA8OC4yLE8e2QDEjL31JAXAJDcUTRTwiQL5jv_hd9Wze6_Oe19mcl1RKn1-z_96riylD3VrwqAR5KkmkyI35WBytAdUU1jpyT1D-RVxX-G3FHoUCgXPDSyvul9Djet65KZE1mkzZfCmo_2s44XcF_Mv4cBfayMdNkodu2EgTsBzgd7lmGszlDhEMZeLDELOIXdQRs5b6g7pt6YRRcGfDo6eRBuR6n9VCES5L9RNizUI--LISnM-W9tWxReGDoj6-YwLFq7bHNt42psvxJO96f3ISwn"",
                        ""x5c"": [
                            ""MIIEoTCCAwmgAwIBAgIUJc25dL8SOWgIIsIUKuHuQuNj/RowDQYJKoZIhvcNAQELBQAwXzELMAkGA1UEBhMCQVUxDDAKBgNVBAgMA0FDVDERMA8GA1UEBwwIQ2FuYmVycmExDTALBgNVBAoMBEFDQ0MxDDAKBgNVBAsMA0NEUjESMBAGA1UEAwwJbWRoLXBzMjU2MCAXDTIyMDUxMzAzMzU0N1oYDzQ0ODYwNjI0MDMzNTQ3WjBfMQswCQYDVQQGEwJBVTEMMAoGA1UECAwDQUNUMREwDwYDVQQHDAhDYW5iZXJyYTENMAsGA1UECgwEQUNDQzEMMAoGA1UECwwDQ0RSMRIwEAYDVQQDDAltZGgtcHMyNTYwggGiMA0GCSqGSIb3DQEBAQUAA4IBjwAwggGKAoIBgQCa6J1AvqH1CLNuJnFmLeunA1UPLuZeO1yX4LaukjglnOjSfbH7coAegK65HtlxfdvYOB+QjYgH02yyl21mwCAo7QxgNE4KfxOsg8qW8/gzxjA1DWah0tL0h9WplTpOn+Zamfz2mjuhmJMar6qKG/7EFRDAcz0UemU8/oO8DliXlRQcN03az2qeh2MEjdIDK5U5jqz1f2K/7AMFi8j/3zWcRgDs12Cb5DFbqofQWADw4LjIsTx7ZAMSMvfUkBcAkNxRNFPCJAvmO/+F31bN7r857X2ZyXVEqfX7P/3quLKUPdWvCoBHkqSaTIjflYHK0B1RTWOnJPUP5FXFf4bcUehQKBc8NLK+6X0ON63rkpkTWaTNl8Kaj/azjhdwX8y/hwF9rIx02Sh27YSBOwHOB3uWYazOUOEQxl4sMQs4hd1BGzlvqDum3phFFwZ8Ojp5EG5Hqf1UIRLkv1E2LNQj74shKcz5b21bFF4YOiPr5jAsWrtsc23jamy/Ek73p/chLCcCAwEAAaNTMFEwHQYDVR0OBBYEFIoSnOlQkoN2QYRyONIKbOWAccZHMB8GA1UdIwQYMBaAFIoSnOlQkoN2QYRyONIKbOWAccZHMA8GA1UdEwEB/wQFMAMBAf8wDQYJKoZIhvcNAQELBQADggGBAFaCZHTEzwAuY4PWsI7t2B0Szm4UAJX264d0TvmEm8WYmY+bYYRFWvwCxa254wk5jMscRpa5S+B1F9K1Lz7K+IdYtJpTu/z0Yzw/b0Rrd3epM25u8Fdx++geLDjZVxeaTPqyS1o3h5TWMcg7vXp67zy34tKePAb/jZezrmOC5N6XPPGgT5GMel/fFD7G4e1gXoAR7J/SxMmE7qXVzdse7Wi19AAVLjKJAn3+7UJmSSig921kJgzX0GRN6w5p2Pwn0gxSyxk3BWMuc9GlyqP/7bPu8EcKAg53YGnIbzYGyTe9mpADYP/Zk1APZEPx6c1uKtgn6yMqtq8MJRos6lrgTP0MyMXB7nq/yE7ED+A1gbiCosKv65WVdP8c366uCAFRPehIjprFzEgUQuPRrPVs0ylof9BlEMH3T58HAhCo2mZ65eF0nHwz+BTe/WcgBJvLNT1mrmlLKG2dpaU0WVtQbpPMRwacQ4/zPlbW4CibLFMahPhu5sJlVhGevvZcxfyNDA==""
                        ],
                        ""alg"": ""PS256""
                        },
                        {
                        ""kty"": ""EC"",
                        ""use"": ""sig"",
                        ""kid"": ""ED5CB45701699B64B4D562AE39BC652515090198"",
                        ""x5t"": ""7Vy0VwFpm2S01WKuObxlJRUJAZg"",
                        ""x5c"": [
                            ""MIICFTCCAbugAwIBAgIUU5JJVsT64GvIYcC38ngd4OURvZkwCgYIKoZIzj0EAwIwXzELMAkGA1UEBhMCQVUxDDAKBgNVBAgMA0FDVDERMA8GA1UEBwwIQ2FuYmVycmExDTALBgNVBAoMBEFDQ0MxDDAKBgNVBAsMA0NEUjESMBAGA1UEAwwJbWRoLWVzMjU2MCAXDTIyMDUxMzAzMzE1NFoYDzQ0ODYwNjI0MDMzMTU0WjBfMQswCQYDVQQGEwJBVTEMMAoGA1UECAwDQUNUMREwDwYDVQQHDAhDYW5iZXJyYTENMAsGA1UECgwEQUNDQzEMMAoGA1UECwwDQ0RSMRIwEAYDVQQDDAltZGgtZXMyNTYwWTATBgcqhkjOPQIBBggqhkjOPQMBBwNCAAR684b1T+WTEWrCRgild1cQEEp10BHCuhAK6pFUSNShn6IVgctQqdBTGE14BGpPli0mRQFCDAlUwkot3bUp0K+go1MwUTAdBgNVHQ4EFgQUXT33/36K2py+/xeQD8uTJSZyRp4wHwYDVR0jBBgwFoAUXT33/36K2py+/xeQD8uTJSZyRp4wDwYDVR0TAQH/BAUwAwEB/zAKBggqhkjOPQQDAgNIADBFAiBNvruyt6BqJVBz8v2MW+VyEUpyCzbDoNmwH24Zovo+ywIhAPGnC+M2SSngIOlDy8XGCCSXEHlKDOQf1vjuXDDo00iZ""
                        ],
                        ""alg"": ""ES256"",
                        ""x"": ""evOG9U_lkxFqwkYIpXdXEBBKddARwroQCuqRVEjUoZ8"",
                        ""y"": ""ohWBy1Cp0FMYTXgEak-WLSZFAUIMCVTCSi3dtSnQr6A"",
                        ""crv"": ""P-256""
                        }
                    ]
                }";
                await Assertions.AssertHasContentJson(expected, response.Content);

#if DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON
                await WriteJsonToFileAsync($"c:/temp/expected.json", expected);
                await WriteJsonToFileAsync($"c:/temp/actual.json", response.Content);
#endif
            }
        }
    }
}
