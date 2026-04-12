using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belkhedma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceOfferAttributeOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HourlyHoursJson",
                table: "ServiceOffers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "NationalityGroupsJson",
                table: "ServiceOffers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HourlyHoursJson",
                table: "ServiceOffers");

            migrationBuilder.DropColumn(
                name: "NationalityGroupsJson",
                table: "ServiceOffers");
        }
    }
}
