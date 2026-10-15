using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CdrAuthServer.Repository.Migrations
{
    /// <inheritdoc />
    public partial class V007_UpdateGrantSeedDataWithDeterministicDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Grants",
                keyColumn: "Key",
                keyValue: "12345678-1234-1234-1234-111122223333",
                columns: new[] { "CreatedAt", "ExpiresAt" },
                values: new object[] { new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Grants",
                keyColumn: "Key",
                keyValue: "expired-refresh-token",
                columns: new[] { "CreatedAt", "Data", "ExpiresAt" },
                values: new object[] { new DateTime(2022, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "{\"response_type\":\"code\",\"CdrArrangementId\":\"87654321-4321-4321-4321-333344445555\"}", new DateTime(2023, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Grants",
                keyColumn: "Key",
                keyValue: "valid-refresh-token",
                columns: new[] { "CreatedAt", "ExpiresAt" },
                values: new object[] { new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Grants",
                keyColumn: "Key",
                keyValue: "12345678-1234-1234-1234-111122223333",
                columns: new[] { "CreatedAt", "ExpiresAt" },
                values: new object[] { new DateTime(2026, 5, 8, 3, 21, 37, 552, DateTimeKind.Utc).AddTicks(6478), new DateTime(2027, 5, 8, 3, 21, 37, 552, DateTimeKind.Utc).AddTicks(6481) });

            migrationBuilder.UpdateData(
                table: "Grants",
                keyColumn: "Key",
                keyValue: "expired-refresh-token",
                columns: new[] { "CreatedAt", "Data", "ExpiresAt" },
                values: new object[] { new DateTime(2025, 5, 7, 3, 21, 37, 552, DateTimeKind.Utc).AddTicks(7048), "{\"response_type\":\"code\",\"CdrArrangementId\":\"bff2d629-cf6c-47e2-85ca-a30d0be8661d\"}", new DateTime(2026, 5, 7, 3, 21, 37, 552, DateTimeKind.Utc).AddTicks(7049) });

            migrationBuilder.UpdateData(
                table: "Grants",
                keyColumn: "Key",
                keyValue: "valid-refresh-token",
                columns: new[] { "CreatedAt", "ExpiresAt" },
                values: new object[] { new DateTime(2026, 5, 8, 3, 21, 37, 552, DateTimeKind.Utc).AddTicks(7014), new DateTime(2027, 5, 8, 3, 21, 37, 552, DateTimeKind.Utc).AddTicks(7015) });
        }
    }
}
