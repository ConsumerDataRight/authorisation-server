using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CdrAuthServer.Repository.Migrations
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarAnalyzer", "S1192:Define a constant instead of using this literal 'Grants' 6 times.", Justification = "Auto-generated migration file.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarAnalyzer", "CA1861:Prefer 'static readonly' fields over constant array arguments if the called method is called repeatedly and is not mutating the passed array", Justification = "Auto-generated migration file.")]
    public partial class V006SeedSoftwareProductStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Grants",
                keyColumn: "Key",
                keyValue: "12345678-1234-1234-1234-111122223333",
                columns: new[] { "CreatedAt", "ExpiresAt" },
                values: new object[] { new DateTime(2023, 2, 12, 22, 8, 27, 831, DateTimeKind.Utc).AddTicks(8060), new DateTime(2024, 2, 12, 22, 8, 27, 831, DateTimeKind.Utc).AddTicks(8061) });

            migrationBuilder.UpdateData(
                table: "Grants",
                keyColumn: "Key",
                keyValue: "expired-refresh-token",
                columns: new[] { "CreatedAt", "Data", "ExpiresAt" },
                values: new object[] { new DateTime(2022, 2, 11, 22, 8, 27, 831, DateTimeKind.Utc).AddTicks(8643), "{\"response_type\":\"code id_token\",\"CdrArrangementId\":\"84290e1a-919d-4ed9-8e88-c47c283c51ea\"}", new DateTime(2023, 2, 11, 22, 8, 27, 831, DateTimeKind.Utc).AddTicks(8643) });

            migrationBuilder.UpdateData(
                table: "Grants",
                keyColumn: "Key",
                keyValue: "valid-refresh-token",
                columns: new[] { "CreatedAt", "ExpiresAt" },
                values: new object[] { new DateTime(2023, 2, 12, 22, 8, 27, 831, DateTimeKind.Utc).AddTicks(8613), new DateTime(2024, 2, 12, 22, 8, 27, 831, DateTimeKind.Utc).AddTicks(8614) });

            migrationBuilder.UpdateData(
                table: "SoftwareProducts",
                keyColumn: "SoftwareProductId",
                keyValue: "cdr-register",
                column: "Status",
                value: "ACTIVE");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Grants",
                keyColumn: "Key",
                keyValue: "12345678-1234-1234-1234-111122223333",
                columns: new[] { "CreatedAt", "ExpiresAt" },
                values: new object[] { new DateTime(2023, 2, 9, 5, 5, 24, 902, DateTimeKind.Utc).AddTicks(474), new DateTime(2024, 2, 9, 5, 5, 24, 902, DateTimeKind.Utc).AddTicks(476) });

            migrationBuilder.UpdateData(
                table: "Grants",
                keyColumn: "Key",
                keyValue: "expired-refresh-token",
                columns: new[] { "CreatedAt", "Data", "ExpiresAt" },
                values: new object[] { new DateTime(2022, 2, 8, 5, 5, 24, 902, DateTimeKind.Utc).AddTicks(901), "{\"response_type\":\"code id_token\",\"CdrArrangementId\":\"7e265e9f-4af6-4188-b5d5-2b3db35d507c\"}", new DateTime(2023, 2, 8, 5, 5, 24, 902, DateTimeKind.Utc).AddTicks(902) });

            migrationBuilder.UpdateData(
                table: "Grants",
                keyColumn: "Key",
                keyValue: "valid-refresh-token",
                columns: new[] { "CreatedAt", "ExpiresAt" },
                values: new object[] { new DateTime(2023, 2, 9, 5, 5, 24, 902, DateTimeKind.Utc).AddTicks(870), new DateTime(2024, 2, 9, 5, 5, 24, 902, DateTimeKind.Utc).AddTicks(870) });

            migrationBuilder.UpdateData(
                table: "SoftwareProducts",
                keyColumn: "SoftwareProductId",
                keyValue: "cdr-register",
                column: "Status",
                value: "ACITVE");
        }
    }
}
