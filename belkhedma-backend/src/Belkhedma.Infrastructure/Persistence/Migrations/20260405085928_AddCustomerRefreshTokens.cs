using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belkhedma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AuthToken",
                table: "CustomerAuthSessions",
                newName: "RefreshToken");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerAuthSessions_AuthToken",
                table: "CustomerAuthSessions",
                newName: "IX_CustomerAuthSessions_RefreshToken");

            migrationBuilder.AddColumn<string>(
                name: "ReplacedByRefreshToken",
                table: "CustomerAuthSessions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAtUtc",
                table: "CustomerAuthSessions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReplacedByRefreshToken",
                table: "CustomerAuthSessions");

            migrationBuilder.DropColumn(
                name: "RevokedAtUtc",
                table: "CustomerAuthSessions");

            migrationBuilder.RenameColumn(
                name: "RefreshToken",
                table: "CustomerAuthSessions",
                newName: "AuthToken");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerAuthSessions_RefreshToken",
                table: "CustomerAuthSessions",
                newName: "IX_CustomerAuthSessions_AuthToken");
        }
    }
}
