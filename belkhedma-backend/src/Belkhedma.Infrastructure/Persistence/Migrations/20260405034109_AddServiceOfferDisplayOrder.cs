using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belkhedma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceOfferDisplayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "ServiceOffers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOffers_ProviderId_DisplayOrder_NameEn",
                table: "ServiceOffers",
                columns: new[] { "ProviderId", "DisplayOrder", "NameEn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceOffers_ProviderId_DisplayOrder_NameEn",
                table: "ServiceOffers");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "ServiceOffers");
        }
    }
}
