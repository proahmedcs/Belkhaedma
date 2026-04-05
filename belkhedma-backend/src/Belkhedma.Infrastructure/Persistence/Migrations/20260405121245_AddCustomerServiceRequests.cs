using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belkhedma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerServiceRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerServiceRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerReference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CustomerSavedLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LocationCity = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LocationDistrict = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LocationLatitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    LocationLongitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    LocationGoogleMapsUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LocationGooglePlaceId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PriceSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceMode = table.Column<int>(type: "int", nullable: false),
                    PackageNameAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    PackageNameEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FinalPriceSar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OriginalPriceSar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    VatAmountSar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    ServiceDate = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    SelectedShift = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SelectedNationality = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SelectedContractDuration = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SelectedWorkersCount = table.Column<int>(type: "int", nullable: true),
                    SelectedHoursPerVisit = table.Column<int>(type: "int", nullable: true),
                    SelectedWeeklyVisits = table.Column<int>(type: "int", nullable: true),
                    SelectedDeliveryWindow = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SelectedProviderSource = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PackageAttributesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerServiceRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerServiceRequests_CustomerAccountId_CreatedAtUtc",
                table: "CustomerServiceRequests",
                columns: new[] { "CustomerAccountId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerServiceRequests_CustomerReference_CreatedAtUtc",
                table: "CustomerServiceRequests",
                columns: new[] { "CustomerReference", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerServiceRequests");
        }
    }
}
