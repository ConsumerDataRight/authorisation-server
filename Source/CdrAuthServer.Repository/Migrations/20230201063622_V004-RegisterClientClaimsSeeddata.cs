using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CdrAuthServer.Repository.Migrations
{
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
