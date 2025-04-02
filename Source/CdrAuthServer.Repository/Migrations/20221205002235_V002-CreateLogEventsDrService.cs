using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CdrAuthServer.Repository.Migrations
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarAnalyzer", "S1192:Define a constant instead of using this literal 'nvarchar(50)' 5 times", Justification = "Auto-generated migration file.")]
#pragma warning disable SA1601 // Partial elements should be documented
    public partial class V002CreateLogEventsDrService : Migration
#pragma warning restore SA1601 // Partial elements should be documented
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogEvents-DrService",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Environment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProcessId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProcessName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ThreadId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MethodName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SourceContext = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEventsDrService", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogEvents-DrService");
        }
    }
}
