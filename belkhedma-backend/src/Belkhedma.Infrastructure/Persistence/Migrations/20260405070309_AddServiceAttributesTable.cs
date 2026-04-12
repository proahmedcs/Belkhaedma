using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belkhedma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceAttributesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceAttributes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttributeKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    OptionSetJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    FilterScope = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceAttributes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceAttributes_ServiceOfferId_AttributeKey",
                table: "ServiceAttributes",
                columns: new[] { "ServiceOfferId", "AttributeKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceAttributes_ServiceOfferId_DisplayOrder_NameEn",
                table: "ServiceAttributes",
                columns: new[] { "ServiceOfferId", "DisplayOrder", "NameEn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceAttributes");
        }
    }
}
