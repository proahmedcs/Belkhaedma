using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belkhedma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderLinksAndJsonDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppUrl",
                table: "Providers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrationModeKey",
                table: "Providers",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Providers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Providers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderType",
                table: "Providers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SupportsB2B",
                table: "Providers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsHourly",
                table: "Providers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsMonthly",
                table: "Providers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsRecruitment",
                table: "Providers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TinyUrl",
                table: "Providers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAtUtc",
                table: "PriceSnapshots",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "ProviderJsonDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProviderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ServiceMode = table.Column<int>(type: "int", nullable: true),
                    JsonContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderJsonDocuments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceSnapshots_ExpiresAtUtc",
                table: "PriceSnapshots",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderJsonDocuments_DocumentKey",
                table: "ProviderJsonDocuments",
                column: "DocumentKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderJsonDocuments_ExpiresAtUtc",
                table: "ProviderJsonDocuments",
                column: "ExpiresAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderJsonDocuments");

            migrationBuilder.DropIndex(
                name: "IX_PriceSnapshots_ExpiresAtUtc",
                table: "PriceSnapshots");

            migrationBuilder.DropColumn(
                name: "AppUrl",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "IntegrationModeKey",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "ProviderType",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "SupportsB2B",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "SupportsHourly",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "SupportsMonthly",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "SupportsRecruitment",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "TinyUrl",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                table: "PriceSnapshots");
        }
    }
}
