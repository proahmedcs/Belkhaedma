using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belkhedma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderAttributeValueMappers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProviderAttributeValueMappers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceMode = table.Column<int>(type: "int", nullable: true),
                    RawAttributeKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RawValue = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    RawTextEn = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    RawTextAr = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    NormalizedAttributeKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NormalizedValue = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    NormalizedTextEn = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    NormalizedTextAr = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderAttributeValueMappers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderAttributeValueMappers_ProviderId_RawAttributeKey_RawValue_IsActive",
                table: "ProviderAttributeValueMappers",
                columns: new[] { "ProviderId", "RawAttributeKey", "RawValue", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderAttributeValueMappers_ProviderId_ServiceOfferId_ServiceMode_RawAttributeKey_RawValue",
                table: "ProviderAttributeValueMappers",
                columns: new[] { "ProviderId", "ServiceOfferId", "ServiceMode", "RawAttributeKey", "RawValue" },
                unique: true,
                filter: "[ServiceOfferId] IS NOT NULL AND [ServiceMode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderAttributeValueMappers");
        }
    }
}
