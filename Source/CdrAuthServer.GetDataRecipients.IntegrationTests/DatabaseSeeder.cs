using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;

#nullable enable

namespace CdrAuthServer.GetDataRecipients.IntegrationTests
{
    public static class DatabaseSeeder
    {
        private enum IndustryType
        {
            Banking,
            Energy,
            Telecommunications,
        }

        private enum ParticipantType
        {
            DH,
            DR,
        }

        private static int nextRegisterLegalEntityId = 0;
        private static int nextRegisterParticipationId = 0;
        private static int nextRegisterBrandId = 0;
        private static int nextRegisterSoftwareProductId = 0;

        private static int nextAuthServerLegalEntityId = 0;
        private static int nextAuthServerBrandId = 0;
        private static int nextAuthServerSoftwareProductId = 0;

        public static async Task Execute(
            int registerLegalEntityCount, int registerBrandCount, int registerSoftwareProductCount,
            int authServerLegalEntityCount, int authServerBrandCount, int authServerSoftwareProductCount,
            bool registerModified,  // simulate change to register records
            bool authServerModified) // simulate change to dataholder records
        {
            // Database is purged so, reset next ids so that ids are consistent across tests
            nextRegisterLegalEntityId = 0;
            nextRegisterParticipationId = 0;
            nextRegisterBrandId = 0;
            nextRegisterSoftwareProductId = 0;
            nextAuthServerLegalEntityId = 0;
            nextAuthServerBrandId = 0;
            nextAuthServerSoftwareProductId = 0;

            // Seed Register
            using var registerConnection = new SqlConnection(BaseTest.CONNECTIONSTRING_REGISTER_RW);
            await registerConnection.OpenAsync();
            await RegisterPurge(registerConnection);
            await RegisterInsert(registerConnection, registerLegalEntityCount, registerBrandCount, registerSoftwareProductCount, registerModified);

            // Seed Software Products
            using var authServerConnection = new SqlConnection(BaseTest.CONNECTIONSTRING_AUTHSERVER_RW);
            await authServerConnection.OpenAsync();
            await AuthServerPurge(authServerConnection);
            await AuthServerInsert(authServerConnection, authServerLegalEntityCount, authServerBrandCount, authServerSoftwareProductCount, authServerModified);
        }

        // Purge register database but leave standing data intact
        private static async Task RegisterPurge(SqlConnection connection)
        {
            await connection.ExecuteAsync("delete AuthDetail");
            await connection.ExecuteAsync("delete Brand");
            await connection.ExecuteAsync("delete Endpoint");
            await connection.ExecuteAsync("delete LegalEntity");
            await connection.ExecuteAsync("delete Participation");
            await connection.ExecuteAsync("delete SoftwareProduct");
            await connection.ExecuteAsync("delete SoftwareProductCertificate");
        }

        // Purge Auth Server database but leave standing data intact
        private static async Task AuthServerPurge(SqlConnection connection)
        {
            await connection.ExecuteAsync("delete SoftwareProducts");
        }

        private static async Task RegisterInsert(SqlConnection connection, int legalEntityCount, int brandCount, int softwareProductCount, bool modified)
        {
            static async Task<Guid> Register_InsertLegalEntity(SqlConnection connection, IndustryType industryType, bool modified)
            {
                var legalEntityId = new Guid($"00000000-0000-0000-0000-{++nextRegisterLegalEntityId:d012}");

                string legalEntityName = $"LegalEntity_{legalEntityId}".ToString().Replace('-', '_');

                await connection.ExecuteScalarAsync<Guid>(
                    @"
                        insert into LegalEntity(LegalEntityId, LegalEntityName, LogoUri, AnzsicDivision, OrganisationTypeId, AccreditationLevelId, AccreditationNumber) 
                        values(@LegalEntityId, @LegalEntityName, @LogoUri, @AnzsicDivision, @OrganisationTypeId, @AccreditationLevelId, @AccreditationNumber)",
                    new
                    {
                        LegalEntityId = legalEntityId,

                        // MA
                        LegalEntityName = legalEntityName,
                        LogoUri = $"https://www.{legalEntityName}.com/logo.jpg",

                        AnzsicDivision = industryType switch
                        {
                            IndustryType.Banking => "6221",
                            IndustryType.Energy => "2640",
                            IndustryType.Telecommunications => "5801",
                            _ => throw new NotSupportedException(),
                        },
                        OrganisationTypeId = "2", // company

                        // LegalEntityStatusId = "1", // make it active by default
                        AccreditationLevelId = "1", // unrestricted
                        AccreditationNumber = $"ABC{nextRegisterLegalEntityId:d012}",
                    });

                return legalEntityId;
            }

            static async Task<Guid> Register_InsertParticipation(SqlConnection connection, Guid legalEntityId, ParticipantType participantType, IndustryType industryType, bool modified)
            {
                var participationId = new Guid($"00000000-0000-0000-0000-{++nextRegisterParticipationId:d012}");

                await connection.ExecuteScalarAsync<Guid>(
                    @"
                    insert into Participation(ParticipationId, LegalEntityId, ParticipationTypeId, IndustryId, StatusId) 
                    values(@ParticipationId, @LegalEntityId,
                        (select ParticipationTypeId from ParticipationType where ParticipationTypeCode = @ParticipantTypeCode),
                        (select IndustryTypeId from IndustryType where IndustryTypeCode = @IndustryTypeCode),
                        (select ParticipationStatusId from ParticipationStatus where Upper(ParticipationStatusCode) = @ParticipationStatusCode))",
                    new
                    {
                        ParticipationId = participationId,
                        LegalEntityId = legalEntityId,
                        ParticipantTypeCode = participantType.ToString(),

                        // MA
                        ParticipationStatusCode = "ACTIVE",

                        IndustryTypeCode = industryType.ToString(),
                    });

                return participationId;
            }

            static async Task<Guid> Register_InsertBrand(SqlConnection connection, Guid participationId, bool modified)
            {
                var brandId = new Guid($"00000000-0000-0000-0000-{++nextRegisterBrandId:d012}");

                string brandName = $"Brand_{brandId}".ToString().Replace('-', '_');

                await connection.ExecuteScalarAsync<Guid>(
                    @"
                    insert into Brand(BrandId, BrandName, LogoUri, BrandStatusId, ParticipationId, LastUpdated) 
                    values(@BrandId, @BrandName, @LogoUri,
                        --(select BrandStatusId from BrandStatus where Upper(BrandStatusCode) = 'ACTIVE'),
                        @StatusId,
                        @ParticipationId,
                        @LastUpdated)",
                    new
                    {
                        BrandId = brandId,

                        // MA
                        BrandName = brandName,
                        LogoUri = $"https://www.{brandName}.com/logo.jpg",
                        StatusId = "1", // 1=active, 2=inactive

                        ParticipationId = participationId,
                        LastUpdated = DateTime.UtcNow,
                    });

                return brandId;
            }

            static async Task<Guid> Register_InsertSoftwareProduct(SqlConnection connection, Guid brandId, bool modified)
            {
                var softwareProductId = new Guid($"00000000-0000-0000-0000-{++nextRegisterSoftwareProductId:d012}");

                string softwareProductName = $"SoftwareProduct_{softwareProductId}".ToString().Replace('-', '_');

                await connection.ExecuteScalarAsync<Guid>(
                    @"
                        insert into SoftwareProduct(
                            SoftwareProductId, 
                            SoftwareProductName, 
                            SoftwareProductDescription, 
                            LogoUri,
                            --SectorIdentifierUri,         --MA
                            ClientUri, 
                            RecipientBaseUri,
                            RevocationUri, 
                            RedirectUris, 
                            JwksUri, 
                            Scope, 
                            StatusId, 
                            BrandId) 
                        values(
                            @SoftwareProductId, 
                            @SoftwareProductName, 
                            @SoftwareProductDescription, 
                            @LogoUri,
                            --@SectorIdentifierUri,         --MA
                            @ClientUri, 
                            @RecipientBaseUri,
                            @RevocationUri, 
                            @RedirectUris, 
                            @JwksUri, 
                            @Scope, 
                            --(select SoftwareProductStatusId from SoftwareProductStatus where Upper(SoftwareProductStatusCode) = 'ACTIVE'), 
                            @StatusId,
                            @BrandId)",
                    new
                    {
                        SoftwareProductId = softwareProductId,

                        // MA
                        SoftwareProductName = $"{softwareProductName}",
                        SoftwareProductDescription = $"{softwareProductName} description",
                        LogoUri = $"https://www.{softwareProductName}.com/logo.jpg",

                        ClientUri = $"https://www.{softwareProductName}.com/client",
                        RecipientBaseUri = $"https://www.{softwareProductName}.com/recipientbase",
                        RevocationUri = $"https://www.{softwareProductName}.com/revocation",
                        RedirectUris = $"https://www.{softwareProductName}.com/redirect1,https://www.{softwareProductName}.com/redirect2",
                        JwksUri = $"https://www.{softwareProductName}.com/jwks",
                        Scope = "scope",

                        // StatusId = modified ? "2" : "1", // 1=active, 2=inactive
                        StatusId = "1", // 1=active, 2=inactive

                        BrandId = brandId,
                    });

                return softwareProductId;
            }

            // Insert legal entities
            for (int ilegalEntity = 0; ilegalEntity < legalEntityCount; ilegalEntity++)
            {
                var register_LegalEntityId = await Register_InsertLegalEntity(connection, IndustryType.Banking, modified);
                var register_ParticipationId = await Register_InsertParticipation(connection, register_LegalEntityId, ParticipantType.DR, IndustryType.Banking, modified);

                // Insert brands
                for (int ibrandCount = 0; ibrandCount < brandCount; ibrandCount++)
                {
                    var register_BrandId = await Register_InsertBrand(connection, register_ParticipationId, modified);

                    // Insert software products
                    for (int isoftwareProductCount = 0; isoftwareProductCount < softwareProductCount; isoftwareProductCount++)
                    {
                        _ = await Register_InsertSoftwareProduct(connection, register_BrandId, modified);
                    }
                }
            }
        }

        private static async Task AuthServerInsert(SqlConnection connection, int legalEntityCount, int brandCount, int softwareProductCount, bool modified)
        {
            static async Task AuthServer_InsertCDRSoftwareProduct(SqlConnection connection)
            {
                await connection.ExecuteScalarAsync(
                    @"
                        insert into SoftwareProducts(SoftwareProductId, SoftwareProductName, SoftwareProductDescription, LogoUri, Status, 
                                                     LegalEntityId, LegalEntityName, LegalEntityStatus, BrandId, BrandName, BrandStatus ) 
                        values(@SoftwareProductId, @SoftwareProductName, @SoftwareProductDescription, @LogoUri, @Status, 
                              @LegalEntityId, @LegalEntityName, @LegalEntityStatus,  @BrandId, @BrandName, @BrandStatus)",
                    new
                    {
                        SoftwareProductId = "cdr-register",
                        SoftwareProductName = "cdr-register",
                        SoftwareProductDescription = "Mock Register",
                        LogoUri = "https://cdrsandbox.gov.au/logo192.png",
                        Status = "ACTIVE",

                        LegalEntityId = "cdr-register",
                        LegalEntityName = "cdr-register",
                        LegalEntityStatus = "ACTIVE",

                        BrandId = "cdr-register",
                        BrandName = "cdr-register",
                        BrandStatus = "ACTIVE",
                    });
            }

            static async Task<Guid> AuthServer_InsertSoftwareProducts(SqlConnection connection, Guid legalEntityId, Guid brandId, bool modified)
            {
                var softwareProductId = new Guid($"00000000-0000-0000-0000-{++nextAuthServerSoftwareProductId:d012}");

                string softwareProductName = $"SoftwareProduct_{softwareProductId}".ToString().Replace('-', '_');

                await connection.ExecuteScalarAsync<Guid>(
                    @"
                        insert into SoftwareProducts(SoftwareProductId, SoftwareProductName, SoftwareProductDescription, LogoUri, Status, 
                                                     LegalEntityId, LegalEntityName, LegalEntityStatus, BrandId, BrandName, BrandStatus ) 
                        values(@SoftwareProductId, @SoftwareProductName, @SoftwareProductDescription, @LogoUri, @Status, 
                              @LegalEntityId, @LegalEntityName, @LegalEntityStatus,  @BrandId, @BrandName, @BrandStatus)",
                    new
                    {
                        SoftwareProductId = softwareProductId,
                        SoftwareProductName = softwareProductName,
                        SoftwareProductDescription = $"{softwareProductName} description",
                        LogoUri = $"https://www.{softwareProductName}.com/logo.jpg",
                        Status = "ACTIVE",

                        LegalEntityId = legalEntityId,
                        LegalEntityName = $"LegalEntity_{legalEntityId}".ToString().Replace('-', '_'),
                        LegalEntityStatus = "ACTIVE",

                        BrandId = brandId,
                        BrandName = $"Brand_{brandId}".ToString().Replace('-', '_'),
                        BrandStatus = "ACTIVE",
                    });

                return softwareProductId;
            }

            await AuthServer_InsertCDRSoftwareProduct(connection);

            for (int i = 1; i <= legalEntityCount; i++)
            {
                var legalEntityId = new Guid($"00000000-0000-0000-0000-{++nextAuthServerLegalEntityId:d012}");
                for (int i2 = 1; i2 <= brandCount; i2++)
                {
                    var brandId = new Guid($"00000000-0000-0000-0000-{++nextAuthServerBrandId:d012}");
                    for (int i3 = 1; i3 <= brandCount; i3++)
                    {
                        _ = await AuthServer_InsertSoftwareProducts(connection, legalEntityId, brandId, modified);
                    }
                }
            }
        }
    }
}
