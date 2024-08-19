using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CdrAuthServer.Repository.Migrations
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarAnalyzer", "CA1861:Prefer 'static readonly' fields over constant array arguments if the called method is called repeatedly and is not mutating the passed array", Justification = "Auto-generated migration file.")]
    public partial class V004RegisterClientClaimsSeeddata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ClientClaims",
                columns: new[] { "ClientId", "Type", "Value" },
                values: new object[] { "cdr-register", "software_id", "cdr-register" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ClientClaims",
                keyColumns: new[] { "ClientId", "Type" },
                keyValues: new object[] { "cdr-register", "software_id" });
        }
    }
}
