using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CdrAuthServer.Repository.Migrations
{
    /// <summary>
    /// Add tables used for MSSQL Logging sinks so that they do not need to be created (too early) by the sink itself during application bootstrapping prior to EF running and creating the destination databases.
    /// </summary>
    public partial class AddLoggingTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the logging table used in the GetDataRecipients function app
            migrationBuilder.Sql("""
                                IF OBJECT_ID('dbo.LogEvents-DrService', 'U') IS NULL
                                BEGIN
                                    CREATE TABLE [dbo].[LogEvents-DrService] (
                                        [Id]            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                        [Message]       NVARCHAR(MAX) NULL,
                                        [Level]         NVARCHAR(MAX) NULL,
                                        [TimeStamp]     DATETIME2(7) NOT NULL,
                                        [Exception]     NVARCHAR(MAX) NULL,
                                        [Environment]   NVARCHAR(50) NULL,
                                        [ProcessId]     NVARCHAR(50) NULL,
                                        [ProcessName]   NVARCHAR(50) NULL,
                                        [ThreadId]      NVARCHAR(50) NULL,
                                        [MethodName]    NVARCHAR(50) NULL,
                                        [SourceContext] NVARCHAR(100) NULL
                                    );
                                END
                                """);

            // Create the logging table used by Auth server
            migrationBuilder.Sql("""
                                IF OBJECT_ID('dbo.LogEvents-AuthServer', 'U') IS NULL
                                BEGIN
                                    CREATE TABLE [dbo].[LogEvents-AuthServer] (
                                        [Id]            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                        [Message]       NVARCHAR(MAX) NULL,
                                        [Level]         NVARCHAR(MAX) NULL,
                                        [TimeStamp]     DATETIME NULL,
                                        [Exception]     NVARCHAR(MAX) NULL,
                                        [Environment]   NVARCHAR(50) NULL,
                                        [ProcessId]     NVARCHAR(50) NULL,
                                        [ProcessName]   NVARCHAR(50) NULL,
                                        [ThreadId]      NVARCHAR(50) NULL,
                                        [MethodName]    NVARCHAR(50) NULL,
                                        [SourceContext] NVARCHAR(100) NULL
                                    );
                                END
                                """);

            // Create the logging table used by the request/response logger.
            migrationBuilder.Sql("""
                                IF OBJECT_ID('dbo.LogEvents-RequestResponse', 'U') IS NULL
                                BEGIN
                                    CREATE TABLE [dbo].[LogEvents-RequestResponse] (
                                        [Id]                 INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                        [Message]            NVARCHAR(MAX) NULL,
                                        [Level]              NVARCHAR(MAX) NULL,
                                        [TimeStamp]          DATETIME NULL,
                                        [Exception]          NVARCHAR(MAX) NULL,
                                        [SourceContext]      VARCHAR(100) NULL,
                                        [ClientId]           VARCHAR(50) NULL,
                                        [SoftwareId]         VARCHAR(50) NULL,
                                        [FapiInteractionId]  VARCHAR(50) NULL,
                                        [RequestMethod]      VARCHAR(20) NULL,
                                        [RequestBody]        VARCHAR(MAX) NULL,
                                        [RequestHeaders]     VARCHAR(MAX) NULL,
                                        [RequestPath]        VARCHAR(2000) NULL,
                                        [RequestQueryString] VARCHAR(4000) NULL,
                                        [StatusCode]         VARCHAR(20) NULL,
                                        [ElapsedTime]        VARCHAR(20) NULL,
                                        [RequestHost]        VARCHAR(4000) NULL,
                                        [RequestIpAddress]   VARCHAR(50) NULL,
                                        [ResponseHeaders]    VARCHAR(4000) NULL,
                                        [ResponseBody]       VARCHAR(MAX) NULL
                                    );
                                END
                                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty.
        }
    }
}
