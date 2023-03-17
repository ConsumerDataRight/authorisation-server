using CdrAuthServer.IntegrationTests.Extensions;
using CdrAuthServer.IntegrationTests.Infrastructure.API2;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;

#nullable enable

namespace CdrAuthServer.IntegrationTests.Fixtures
{
    /// <summary>
    /// Methods for setting up tests
    /// </summary>
    public static class TestSetup
    {
        /// <summary>
        /// The seed data for the Register is using the loopback uri for redirecturi.
        /// Since the integration tests stands up it's own data recipient consent/callback endpoint we need to 
        /// patch the redirect uri to match our callback.
        /// </summary>
        static public void Register_PatchRedirectUri(
            string softwareProductId = BaseTest.SOFTWAREPRODUCT_ID,
            string redirectURI = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS)
        {
            redirectURI = BaseTest.SubstituteConstant(redirectURI);

            using var connection = new SqlConnection(BaseTest.REGISTER_CONNECTIONSTRING);
            connection.Open();

            using var updateCommand = new SqlCommand("update softwareproduct set redirecturis = @uri where lower(softwareproductid) = @id", connection);
            updateCommand.Parameters.AddWithValue("@uri", redirectURI);
            updateCommand.Parameters.AddWithValue("@id", softwareProductId.ToLower());
            updateCommand.ExecuteNonQuery();

            using var selectCommand = new SqlCommand($"select redirecturis from softwareproduct where lower(softwareproductid) = @id", connection);
            selectCommand.Parameters.AddWithValue("@id", softwareProductId.ToLower());
            if (selectCommand.ExecuteScalarString() != redirectURI)
            {
                throw new Exception($"softwareproduct.redirecturis is not '{redirectURI}'");
            }
        }

        /// <summary>
        /// The seed data for the Register is using the loopback uri for jwksuri.
        /// Since the integration tests stands up it's own data recipient jwks endpoint we need to 
        /// patch the jwks uri to match our endpoint.
        /// </summary>
        static public void Register_PatchJwksUri(
            string softwareProductId = BaseTest.SOFTWAREPRODUCT_ID,
            string jwksURI = BaseTest.SOFTWAREPRODUCT_JWKS_URI_FOR_INTEGRATION_TESTS)
        {
            jwksURI = BaseTest.SubstituteConstant(jwksURI);

            using var connection = new SqlConnection(BaseTest.REGISTER_CONNECTIONSTRING);
            connection.Open();

            using var updateCommand = new SqlCommand("update softwareproduct set jwksuri = @uri where lower(softwareproductid) = @id", connection);
            updateCommand.Parameters.AddWithValue("@uri", jwksURI);
            updateCommand.Parameters.AddWithValue("@id", softwareProductId.ToLower());
            updateCommand.ExecuteNonQuery();

            using var selectCommand = new SqlCommand($"select jwksuri from softwareproduct where lower(softwareproductid) = @id", connection);
            selectCommand.Parameters.AddWithValue("@id", softwareProductId.ToLower());
            if (selectCommand.ExecuteScalarString() != jwksURI)
            {
                throw new Exception($"softwareproduct.jwksuri is not '{jwksURI}'");
            }
        }

        /// <summary>
        /// Clear data from the Dataholder's IdentityServer database
        /// </summary>
        /// <param name="onlyPersistedGrants">Only clear the persisted grants table</param>
        static public void DataHolder_PurgeIdentityServer(bool onlyPersistedGrants = false)
        {
            using var connection = new SqlConnection(BaseTest.IDENTITYSERVER_CONNECTIONSTRING);

            void Purge(string table)
            {
                // Delete all rows
                using var deleteCommand = new SqlCommand($"delete from {table}", connection);
                deleteCommand.ExecuteNonQuery();

                // Check all rows deleted
                using var selectCommand = new SqlCommand($"select count(*) from {table}", connection);
                var count = selectCommand.ExecuteScalarInt32();
                if (count != 0)
                {
                    throw new Exception($"Table {table} was not purged");
                }
            }

            connection.Open();

            if (!onlyPersistedGrants)
            {
                // Purge("ApiResourceClaims");
                // Purge("ApiResourceProperties");
                // Purge("ApiResources");
                // Purge("ApiResourceScopes");
                // Purge("ApiResourceSecrets");
                // Purge("ApiScopeClaims");
                // Purge("ApiScopeProperties");
                // Purge("ApiScopes");
                // Purge("ClientClaims");
                // Purge("ClientCorsOrigins");
                // Purge("ClientGrantTypes");
                // Purge("ClientIdPRestrictions");
                // Purge("ClientPostLogoutRedirectUris");
                // Purge("ClientProperties");
                // Purge("ClientRedirectUris");
                // Purge("Clients");
                // Purge("ClientScopes");
                // Purge("ClientSecrets");
                // Purge("DeviceCodes");
                // Purge("IdentityResourceClaims");
                // Purge("IdentityResourceProperties");
                // Purge("IdentityResources");

                // FIXME - MJS - Check database, see if other tables have been added
                Purge("ClientClaims");
                Purge("Clients");
            }

            // Purge("PersistedGrants");
            Purge("Grants");


            // FIXME - MJS 
            // Purge("SoftwareProducts");
        }

        // Get SSA from the Register and register it with the DataHolder
        static public async Task<(string ssa, string registration, string clientId)> DataHolder_RegisterSoftwareProduct(
            string brandId = BaseTest.BRANDID,
            string softwareProductId = BaseTest.SOFTWAREPRODUCT_ID,
            string jwtCertificateFilename = BaseTest.JWT_CERTIFICATE_FILENAME,
            string jwtCertificatePassword = BaseTest.JWT_CERTIFICATE_PASSWORD)
        {
            // Get SSA from Register
            var ssa = await Register_SSA_API.GetSSA(brandId, softwareProductId, "3", jwtCertificateFilename, jwtCertificatePassword);

            // Register software product with DataHolder
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa,
                jwtCertificateFilename: jwtCertificateFilename,
                jwtCertificatePassword: jwtCertificatePassword,
                responseType: "code,code id_token",
                authorization_signed_response_alg: "PS256");

            // var response = await US15221_US12969_US15586_MDH_InfosecProfileAPI_Registration_Base.RegisterSoftwareProduct(registrationRequest);
            var response = await DataHolder_Register_API.RegisterSoftwareProduct(registrationRequest);
            if (response.StatusCode != HttpStatusCode.Created)
            {
                throw new Exception($"Unable to register software product - {softwareProductId} - Response.StatusCode={response.StatusCode}, Response.Content={await response.Content.ReadAsStringAsync()}");
            }

            var registration = await response.Content.ReadAsStringAsync();

            // Extract clientId from registration
            var clientId = JsonConvert.DeserializeObject<dynamic>(registration).client_id.ToString();
            if (string.IsNullOrEmpty(clientId))
            {
                throw new NullReferenceException(nameof(clientId));
            }

            return (ssa, registration, clientId);
        }


        private class SoftwareProduct
        {
            public string? SoftwareProductId { get; set; }
            public string? SoftwareProductName { get; set; }
            public string? SoftwareProductDescription { get; set; }
            public string? LogoUri { get; set; }
            public string? Status { get; set; }
            public string? LegalEntityId { get; set; }
            public string? LegalEntityName { get; set; }
            public string? LegalEntityStatus { get; set; }
            public string? BrandId { get; set; }
            public string? BrandName { get; set; }
            public string? BrandStatus { get; set; }
        }

        static public void CdrAuthServer_SeedDatabase()
        {
            using var connection = new SqlConnection(BaseTest.IDENTITYSERVER_CONNECTIONSTRING);

            connection.Query("delete softwareproducts");
            if (connection.QuerySingle<int>("select count(*) from softwareproducts") != 0)
            {
                throw new Exception("Unable to delete softwareproducts");
            }

            connection.Insert(new SoftwareProduct()
            {
                SoftwareProductId = BaseTest.SOFTWAREPRODUCT_ID,
                SoftwareProductName = "Mock Data Recipient Software Product",
                SoftwareProductDescription = "Mock Data Recipient Software Product",
                LogoUri = "https://cdrsandbox.gov.au/logo192.png",
                Status = "ACTIVE",
                LegalEntityId = "18B75A76-5821-4C9E-B465-4709291CF0F4",
                LegalEntityName = "Mock Data Recipient Legal Entity Name",
                LegalEntityStatus = "ACTIVE",
                BrandId = BaseTest.BRANDID,
                BrandName = "Mock Data Recipient Brand Name",
                BrandStatus = "ACTIVE"
            });

            connection.Insert(new SoftwareProduct()
            {
                SoftwareProductId = BaseTest.ADDITIONAL_SOFTWAREPRODUCT_ID,
                SoftwareProductName = "Track Xpense",
                SoftwareProductDescription = "Application to allow you to track your expenses",
                LogoUri = "https://cdrsandbox.gov.au/foo.png",
                Status = "ACTIVE",
                LegalEntityId = "9d34ede4-2c76-4ecc-a31e-ea8392d31cc9",
                LegalEntityName = "FintechX",
                LegalEntityStatus = "ACTIVE",
                BrandId = BaseTest.ADDITIONAL_BRAND_ID,
                BrandName = "Finance X",
                BrandStatus = "ACTIVE"
            });
        }
    }
}
