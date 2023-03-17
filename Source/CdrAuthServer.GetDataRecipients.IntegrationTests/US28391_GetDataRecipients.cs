// #define DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON

using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using Newtonsoft.Json;
using Xunit;
using FluentAssertions;
using FluentAssertions.Execution;
using System.Net.Http;
using System.Net;

#nullable enable

namespace CdrAuthServer.GetDataRecipients.IntegrationTests
{
    // 28724
    [CollectionDefinition("Non-Parallel Collection", DisableParallelization = true)]
    public class US28391_GetDataRecipients : BaseTest
    {
        private async Task ExecuteAzureFunction()
        {
            var client = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Get, $"{AZUREFUNCTIONS_URL}/INTEGRATIONTESTS_DATARECIPIENTS");

            var response = await client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Expected OK calling {request.RequestUri} but got {response.StatusCode}");
            }
        }

        private async Task Test(
            int registerLegalEntityCount, int registerBrandCount, int registerSoftwareProductCount,
            int authServerLegalEntityCount, int authServerBrandCount, int authServerSoftwareProductCount,
            bool registerModified = false, bool authServerModified = false)
        {
            // Arrange
            await DatabaseSeeder.Execute(
                registerLegalEntityCount, registerBrandCount, registerSoftwareProductCount,
                authServerLegalEntityCount, authServerBrandCount, authServerSoftwareProductCount,
                registerModified, authServerModified
            );

            // Act
            await ExecuteAzureFunction();

            // Assert
            using (new AssertionScope())
            {
                await Assert_RegisterAndDataHolderIsSynced();
            }
        }

        [Theory]
        [InlineData(0, 0, 0)] // no records
        [InlineData(1, 0, 0)] // has DH legalentity - FAILS - doesnt delete extra legalentity
        [InlineData(1, 1, 0)] // has DH legalentity & brand - FAILS - doesnt delete extra legalentity, brand
        [InlineData(1, 1, 1)] // has DH legalentity & brand & softwareproduct - FAILS - doesnt delete extra legalentity, brand, softwareproduct
        public async Task ACX01_WhenRegisterEmpty_ShouldSync(int authServerLegalEntityCount, int authServerBrandCount, int authServerSoftwareProductCount)
        {
            await Test(0, 0, 0, authServerLegalEntityCount, authServerBrandCount, authServerSoftwareProductCount);
        }

        [Theory]
        [InlineData(0, 0, 0)] // no records
        [InlineData(1, 1, 1)] // has DH legalentity & brand & softwareproduct
        public async Task ACX01_WhenDataHolderEmpty_ShouldSync(int registerLegalEntityCount, int registerBrandCount, int registerSoftwareProductCount)
        {
            await Test(registerLegalEntityCount, registerBrandCount, registerSoftwareProductCount, 0, 0, 0);
        }

        [Theory]
        [InlineData(0, 0, 0)] // nothing
        [InlineData(1, 1, 1)] // has legalentity & brand & softwareproduct
        public async Task ACX01_WhenRegisterAndDataHolderSame_ShouldSync(int legalEntityCount, int brandCount, int softwareProductCount)
        {
            await Test(legalEntityCount, brandCount, softwareProductCount, legalEntityCount, brandCount, softwareProductCount);
        }

        [Fact]
        public async Task ACX01_WhenDataHolderChanged_ShouldSync()
        {
            await Test(1, 1, 1, 1, 1, 1, false, true);
        }

        

        static private async Task Assert_RegisterAndDataHolderIsSynced()
        {
            static async Task Assert_TableDataIsEqual(
                SqlConnection registerConnection, string registerSql,
                SqlConnection authServerConnection, string authServerSql,
                string tableName)
            {

                var registerJson = JsonConvert.SerializeObject(await registerConnection.QueryAsync(registerSql));
                var authServerJson = JsonConvert.SerializeObject(await authServerConnection.QueryAsync(authServerSql));

                // Assert data is same
#if DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON
                File.WriteAllText($"c:/temp/expected_{tableName}.json", registerJson);
                File.WriteAllText($"c:/temp/actual_{tableName}.json", authServerJson);                
#endif
                authServerJson.Should().Be(registerJson);

            }

            const string REGISTER_LEGALENTITY_SQL = @"
                select 
                    le.LegalEntityId,
                    le.LegalEntityName,
                    -- le.LegalEntityStatusId,
                    Upper(ps.ParticipationStatusCode) Status        --*
                    --le.LogoUri                                -- MA
                    -- p.ParticipationTypeId,
                    -- p.IndustryId,
                    -- p.StatusId,
                    -- pt.ParticipationTypeCode
                from LegalEntity le
                -- left outer join LegalEntityStatus les on les.LegalEntityStatusId = le.LegalEntityStatusId
                left outer join Participation p on p.LegalEntityId = le.LegalEntityId
                left outer join ParticipationStatus ps on ps.ParticipationStatusId = p.StatusId
                left outer join ParticipationType pt on pt.ParticipationTypeId = p.ParticipationTypeId
                where pt.ParticipationTypeCode = 'DR'
                order by le.LegalEntityId";

            const string REGISTER_BRAND_SQL = @"
                select 
                    b.BrandId, 
                    b.BrandName,
                    --b.LogoUri,                                -- MA
                    bs.BrandStatusCode Status,
                    le.LegalEntityId LegalEntityId
                from Brand b
                left outer join Participation p on p.ParticipationId = b.ParticipationId
                left outer join ParticipationType pt on pt.ParticipationTypeId = p.ParticipationTypeId                
                left outer join LegalEntity le on le.LegalEntityId = p.LegalEntityId
                left outer join BrandStatus bs on bs.BrandStatusId = b.BrandStatusId
                where pt.ParticipationTypeCode = 'DR'                
                order by BrandId";

            const string REGISTER_SOFTWAREPRODUCT_SQL = @"
                select 
                    sp.SoftwareProductId,
                    sp.SoftwareProductName,
                    sp.SoftwareProductDescription,
                    sp.LogoUri,           
                    sps.SoftwareProductStatusCode Status
                from SoftwareProduct sp
                left outer join Brand b on b.BrandId = sp.BrandId
                left outer join Participation p on p.ParticipationId = b.ParticipationId
                left outer join ParticipationType pt on pt.ParticipationTypeId = p.ParticipationTypeId                
                left outer join SoftwareProductStatus sps on sps.SoftwareProductStatusId = sp.StatusId
                where pt.ParticipationTypeCode = 'DR' -- hardly necessary since only DRs have software products anyway
                order by SoftwareProductId";

            //Upper(Status) Status,
            var AUTHSERVER_LEGALENTITY_SQL = "select LegalEntityId, LegalEntityName, LegalEntityStatus Status from SoftwareProducts where LegalEntityId != 'cdr-register' order by LegalEntityId ";
            var AUTHSERVER_BRAND_SQL = "select BrandId, BrandName, BrandStatus Status, LegalEntityId from SoftwareProducts where BrandId != 'cdr-register' order by BrandId";
            var AUTHSERVER_SOFTWAREPRODUCT_SQL = "select SoftwareProductId, SoftwareProductName, SoftwareProductDescription, LogoUri, Status from SoftwareProducts  where SoftwareProductId != 'cdr-register' order by SoftwareProductId";

            using var registerConnection = new SqlConnection(BaseTest.CONNECTIONSTRING_REGISTER_RW);
            registerConnection.Open();

            using var authServerConnection = new SqlConnection(BaseTest.CONNECTIONSTRING_AUTHSERVER_RW);
            authServerConnection.Open();

            // Assert
            await Assert_TableDataIsEqual(registerConnection, REGISTER_LEGALENTITY_SQL, authServerConnection, AUTHSERVER_LEGALENTITY_SQL, "LegalEntity");
            await Assert_TableDataIsEqual(registerConnection, REGISTER_BRAND_SQL, authServerConnection, AUTHSERVER_BRAND_SQL, "Brand");
            await Assert_TableDataIsEqual(registerConnection, REGISTER_SOFTWAREPRODUCT_SQL, authServerConnection, AUTHSERVER_SOFTWAREPRODUCT_SQL, "SoftwareProducts");
        }
    }
}
