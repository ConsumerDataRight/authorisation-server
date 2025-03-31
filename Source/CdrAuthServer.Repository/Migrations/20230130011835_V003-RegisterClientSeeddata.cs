using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using System.Configuration;

namespace CdrAuthServer.Repository.Migrations
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarAnalyzer", "S1192:Define a constant instead of using this literal 'cdr-register' 4 times.", Justification = "Auto-generated migration file.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarAnalyzer", "CA1861:Prefer 'static readonly' fields over constant array arguments if the called method is called repeatedly and is not mutating the passed array", Justification = "Auto-generated migration file.")]
#pragma warning disable SA1601 // Partial elements should be documented
    public partial class V003RegisterClientSeeddata : Migration
#pragma warning restore SA1601 // Partial elements should be documented
    {
        public static string SSA_JWKS_URI => Configuration["CdrAuthServer:CdrRegister:SsaJwksUri"]
            ?? throw new ConfigurationErrorsException($"{nameof(SSA_JWKS_URI)} - configuration setting not found");

        // SETTING UP CONFIGURATION BUILDER FOR REGISTER SSA JWKS URI
        private static IConfigurationRoot? configuration;

        public static IConfigurationRoot Configuration
        {
            get
            {
                configuration ??= new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                        .AddEnvironmentVariables()
                        .Build();

                return configuration;
            }
        }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Dynamic SSA_JWKS_URI for seed data
            migrationBuilder.InsertData(
                table: "Clients",
                columns: ["ClientId", "ClientDescription", "ClientIdIssuedAt", "ClientName"],
                values: ["cdr-register", null, 0L, "CDR Register"]);

            migrationBuilder.InsertData(
                table: "ClientClaims",
                columns: ["ClientId", "Type", "Value"],
                values: ["cdr-register", "JwksUri", $"{SSA_JWKS_URI}"]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ClientClaims",
                keyColumns: ["ClientId", "Type"],
                keyValues: ["cdr-register", "JwksUri"]);

            migrationBuilder.DeleteData(
                table: "Clients",
                keyColumn: "ClientId",
                keyValue: "cdr-register");
        }
    }
}
