using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CdrAuthServer.Repository.Migrations
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarAnalyzer", "CA1861:Prefer 'static readonly' fields over constant array arguments if the called method is called repeatedly and is not mutating the passed array", Justification = "Auto-generated migration file.")]
#pragma warning disable SA1601 // Partial elements should be documented
    public partial class V004RegisterClientClaimsSeeddata : Migration
#pragma warning restore SA1601 // Partial elements should be documented
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ClientClaims",
                columns: ["ClientId", "Type", "Value"],
                values: ["cdr-register", "software_id", "cdr-register"]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ClientClaims",
                keyColumns: ["ClientId", "Type"],
                keyValues: ["cdr-register", "software_id"]);
        }
    }
}
