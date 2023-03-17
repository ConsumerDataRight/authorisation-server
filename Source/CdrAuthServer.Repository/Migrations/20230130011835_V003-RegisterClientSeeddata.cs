using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using System.Configuration;

#nullable disable

namespace CdrAuthServer.Repository.Migrations
{
    public partial class V003RegisterClientSeeddata : Migration
    {        
        public static string SSA_JWKS_URI => Configuration["CdrAuthServer:CdrRegister:SsaJwksUri"]
            ?? throw new ConfigurationErrorsException($"{nameof(SSA_JWKS_URI)} - configuration setting not found");

        // SETTING UP CONFIGURATION BUILDER FOR REGISTER SSA JWKS URI
        static private IConfigurationRoot? configuration;
        static public IConfigurationRoot Configuration
        {
            get
            {
                if (configuration == null)
                {
                    configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                        .AddEnvironmentVariables()
                        .Build();
                }

                return configuration;
            }
        }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Dynamic SSA_JWKS_URI for seed data            
            migrationBuilder.InsertData(
                table: "Clients",
                columns: new[] { "ClientId", "ClientDescription", "ClientIdIssuedAt", "ClientName" },
                values: new object[] { "cdr-register", null, 0L, "CDR Register" });
            
            migrationBuilder.InsertData(
                table: "ClientClaims",
                columns: new[] { "ClientId", "Type", "Value" },
                values: new object[] { "cdr-register", "JwksUri", $"{SSA_JWKS_URI}" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ClientClaims",
                keyColumns: new[] { "ClientId", "Type" },
                keyValues: new object[] { "cdr-register", "JwksUri" });

            migrationBuilder.DeleteData(
                table: "Clients",
                keyColumn: "ClientId",
                keyValue: "cdr-register");
        }
    }
}
