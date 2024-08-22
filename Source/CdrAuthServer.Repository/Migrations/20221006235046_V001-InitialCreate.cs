using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace CdrAuthServer.Repository.Migrations
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarAnalyzer", "S1192:Define a constant instead of using this literal 'nvarchar(450)' 5 times", Justification = "Auto-generated migration file.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarAnalyzer", "CA1861:Prefer 'static readonly' fields over constant array arguments if the called method is called repeatedly and is not mutating the passed array", Justification = "Auto-generated migration file.")]
    public partial class V001InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientIdIssuedAt = table.Column<long>(type: "bigint", nullable: false),
                    ClientName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientDescription = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.ClientId);
                });

            migrationBuilder.CreateTable(
                name: "Grants",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GrantType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubjectId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Scope = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grants", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "SoftwareProducts",
                columns: table => new
                {
                    SoftwareProductId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SoftwareProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoftwareProductDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogoUri = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LegalEntityId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LegalEntityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LegalEntityStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BrandId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BrandName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BrandStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareProducts", x => x.SoftwareProductId);
                });

            migrationBuilder.CreateTable(
                name: "Tokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BlackListed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClientClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientClaims_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Clients",
                columns: new[] { "ClientId", "ClientDescription", "ClientIdIssuedAt", "ClientName" },
                values: new object[] { "11111111-1111-1111-1111-111111111111", null, 0L, "Software Product 1" });

            migrationBuilder.InsertData(
                table: "Grants",
                columns: new[] { "Key", "ClientId", "CreatedAt", "Data", "ExpiresAt", "GrantType", "Scope", "SubjectId", "UsedAt" },
                values: new object[,]
                {
                    { "12345678-1234-1234-1234-111122223333", "c6327f87-687a-4369-99a4-eaacd3bb8210", new DateTime(2022, 10, 6, 23, 50, 45, 993, DateTimeKind.Utc).AddTicks(9273), "{\"refresh_token\":\"valid-refresh-token\",\"account_id\":[\"123\",\"456\",\"789\"]}", new DateTime(2023, 10, 6, 23, 50, 45, 993, DateTimeKind.Utc).AddTicks(9277), "cdr_arrangement", "openid profile cdr:registration common:customer.basic:read common:customer.detail:read bank:accounts.basic:read bank:accounts.detail:read bank:transactions:read bank:payees:read bank:regular_payments:read energy:electricity.servicepoints.basic:read energy:electricity.servicepoints.detail:read energy:electricity.usage:read energy:electricity.der:read energy:accounts.basic:read energy:accounts.detail:read energy:accounts.paymentschedule:read energy:accounts.concessions:read energy:billing:read", "customer1", null },
                    { "expired-refresh-token", "c6327f87-687a-4369-99a4-eaacd3bb8210", new DateTime(2021, 10, 5, 23, 50, 45, 993, DateTimeKind.Utc).AddTicks(9729), "{\"response_type\":\"code id_token\",\"CdrArrangementId\":\"5331d338-6ee8-4d14-b670-fbdb62d6837f\"}", new DateTime(2022, 10, 5, 23, 50, 45, 993, DateTimeKind.Utc).AddTicks(9730), "refresh_token", "openid profile cdr:registration common:customer.basic:read common:customer.detail:read bank:accounts.basic:read bank:accounts.detail:read bank:transactions:read bank:payees:read bank:regular_payments:read", "customer1", null },
                    { "valid-refresh-token", "c6327f87-687a-4369-99a4-eaacd3bb8210", new DateTime(2022, 10, 6, 23, 50, 45, 993, DateTimeKind.Utc).AddTicks(9704), "{\"response_type\":\"code id_token\",\"CdrArrangementId\":\"12345678-1234-1234-1234-111122223333\"}", new DateTime(2023, 10, 6, 23, 50, 45, 993, DateTimeKind.Utc).AddTicks(9705), "refresh_token", "openid profile cdr:registration common:customer.basic:read common:customer.detail:read bank:accounts.basic:read bank:accounts.detail:read bank:transactions:read bank:payees:read bank:regular_payments:read energy:electricity.servicepoints.basic:read energy:electricity.servicepoints.detail:read energy:electricity.usage:read energy:electricity.der:read energy:accounts.basic:read energy:accounts.detail:read energy:accounts.paymentschedule:read energy:accounts.concessions:read energy:billing:read", "customer1", null }
                });

            migrationBuilder.InsertData(
                table: "SoftwareProducts",
                columns: new[] { "SoftwareProductId", "BrandId", "BrandName", "BrandStatus", "LegalEntityId", "LegalEntityName", "LegalEntityStatus", "LogoUri", "SoftwareProductDescription", "SoftwareProductName", "Status" },
                values: new object[,]
                {
                    { "22222222-2222-2222-2222-222222222222", "BBBBBBBB-2222-2222-2222-222222222222", "Active Data Recipient Brand Name", "ACTIVE", "LLLLLLLL-2222-2222-2222-222222222222", "Active Data Recipient Legal Entity Name", "ACTIVE", "https://cdrsandbox.gov.au/logo192.png", "Active Data Recipient Software Product", "Active Data Recipient Software Product", "ACTIVE" },
                    { "99999999-9999-9999-9999-999999999999", "BBBBBBBB-2222-2222-2222-222222222222", "Active Data Recipient Brand Name", "ACTIVE", "LLLLLLLL-2222-2222-2222-222222222222", "Active Data Recipient Legal Entity Name", "ACTIVE", "https://cdrsandbox.gov.au/logo192.png", "Removed Software Product", "Removed Software Product", "REMOVED" },
                    { "c6327f87-687a-4369-99a4-eaacd3bb8210", "FFB1C8BA-279E-44D8-96F0-1BC34A6B436F", "Mock Data Recipient Brand Name", "ACTIVE", "18B75A76-5821-4C9E-B465-4709291CF0F4", "Mock Data Recipient Legal Entity Name", "ACTIVE", "https://cdrsandbox.gov.au/logo192.png", "Mock Data Recipient Software Product", "Mock Data Recipient Software Product", "ACTIVE" }
                });

            migrationBuilder.InsertData(
                table: "ClientClaims",
                columns: new[] { "Id", "ClientId", "Type", "Value" },
                values: new object[] { 1, "11111111-1111-1111-1111-111111111111", "SoftwareId", "22222222-2222-2222-2222-222222222222" });

            migrationBuilder.InsertData(
                table: "ClientClaims",
                columns: new[] { "Id", "ClientId", "Type", "Value" },
                values: new object[] { 2, "11111111-1111-1111-1111-111111111111", "JwksUri", "https://localhost:9001/jwks" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientClaims_ClientId",
                table: "ClientClaims",
                column: "ClientId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientClaims");

            migrationBuilder.DropTable(
                name: "Grants");

            migrationBuilder.DropTable(
                name: "SoftwareProducts");

            migrationBuilder.DropTable(
                name: "Tokens");

            migrationBuilder.DropTable(
                name: "Clients");
        }
    }
}
