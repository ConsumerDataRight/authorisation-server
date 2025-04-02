using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CdrAuthServer.Repository.Migrations
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarAnalyzer", "S1192:Define a constant instead of using this literal 'Grants' 6 times.", Justification = "Auto-generated migration file.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarAnalyzer", "CA1861:Prefer 'static readonly' fields over constant array arguments if the called method is called repeatedly and is not mutating the passed array", Justification = "Auto-generated migration file.")]
#pragma warning disable SA1601 // Partial elements should be documented
    public partial class V005AddSoftwareProductForCDRRegister : Migration
#pragma warning restore SA1601 // Partial elements should be documented
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Grants",
                keyColumn: "Key",
                keyValue: "12345678-1234-1234-1234-111122223333",
                columns: ["CreatedAt", "ExpiresAt"],
                values: [new DateTime(2023, 2, 9, 5, 5, 24, 902, DateTimeKind.Utc).AddTicks(474), new DateTime(2024, 2, 9, 5, 5, 24, 902, DateTimeKind.Utc).AddTicks(476)]);

            migrationBuilder.UpdateData(
                table: "Grants",
                keyColumn: "Key",
                keyValue: "expired-refresh-token",
                columns: ["CreatedAt", "Data", "ExpiresAt"],
                values: [new DateTime(2022, 2, 8, 5, 5, 24, 902, DateTimeKind.Utc).AddTicks(901), "{\"response_type\":\"code id_token\",\"CdrArrangementId\":\"7e265e9f-4af6-4188-b5d5-2b3db35d507c\"}", new DateTime(2023, 2, 8, 5, 5, 24, 902, DateTimeKind.Utc).AddTicks(902)]);

            migrationBuilder.UpdateData(
                table: "Grants",
                keyColumn: "Key",
                keyValue: "valid-refresh-token",
                columns: ["CreatedAt", "ExpiresAt"],
                values: [new DateTime(2023, 2, 9, 5, 5, 24, 902, DateTimeKind.Utc).AddTicks(870), new DateTime(2024, 2, 9, 5, 5, 24, 902, DateTimeKind.Utc).AddTicks(870)]);

            migrationBuilder.InsertData(
                table: "SoftwareProducts",
                columns: ["SoftwareProductId", "BrandId", "BrandName", "BrandStatus", "LegalEntityId", "LegalEntityName", "LegalEntityStatus", "LogoUri", "SoftwareProductDescription", "SoftwareProductName", "Status"],
                values: ["cdr-register", "cdr-register", "cdr-register", "ACTIVE", "cdr-register", "cdr-register", "ACTIVE", "https://cdrsandbox.gov.au/logo192.png", "Mock Register", "cdr-register", "ACITVE"]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SoftwareProducts",
                keyColumn: "SoftwareProductId",
                keyValue: "cdr-register");

            migrationBuilder.UpdateData(
                table: "Grants",
                keyColumn: "Key",
                keyValue: "12345678-1234-1234-1234-111122223333",
                columns: ["CreatedAt", "ExpiresAt"],
                values: [new DateTime(2023, 2, 1, 6, 36, 21, 885, DateTimeKind.Utc).AddTicks(7040), new DateTime(2024, 2, 1, 6, 36, 21, 885, DateTimeKind.Utc).AddTicks(7042)]);

            migrationBuilder.UpdateData(
                table: "Grants",
                keyColumn: "Key",
                keyValue: "expired-refresh-token",
                columns: ["CreatedAt", "Data", "ExpiresAt"],
                values: [new DateTime(2022, 1, 31, 6, 36, 21, 885, DateTimeKind.Utc).AddTicks(7411), "{\"response_type\":\"code id_token\",\"CdrArrangementId\":\"183c4c56-a7bd-4316-8a4d-cbc40dd9ce53\"}", new DateTime(2023, 1, 31, 6, 36, 21, 885, DateTimeKind.Utc).AddTicks(7411)]);

            migrationBuilder.UpdateData(
                table: "Grants",
                keyColumn: "Key",
                keyValue: "valid-refresh-token",
                columns: ["CreatedAt", "ExpiresAt"],
                values: [new DateTime(2023, 2, 1, 6, 36, 21, 885, DateTimeKind.Utc).AddTicks(7382), new DateTime(2024, 2, 1, 6, 36, 21, 885, DateTimeKind.Utc).AddTicks(7382)]);
        }
    }
}
