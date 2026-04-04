using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belkhedma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerAuthModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM CustomerSavedLocations
                WHERE CustomerReference = 'demo-customer'
                """);

            migrationBuilder.CreateTable(
                name: "CustomerAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerReference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MobileNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NormalizedMobileNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAuthSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthToken = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAuthSessions", x => x.Id);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO CustomerAccounts (Id, CustomerReference, FullName, MobileNumber, NormalizedMobileNumber, IsActive, CreatedAtUtc, UpdatedAtUtc)
                VALUES ('11111111-1111-1111-1111-111111111111', 'demo-customer', 'Demo Customer', '+966500000000', '+966500000000', 1, GETUTCDATE(), GETUTCDATE())
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO CustomerSavedLocations (Id, CustomerReference, Label, City, District, Latitude, Longitude, GoogleMapsUrl, GooglePlaceId, CreatedAtUtc, UpdatedAtUtc)
                VALUES
                ('22222222-2222-2222-2222-222222222221', 'demo-customer', 'Home', 'Riyadh', 'Al Yasmin', 24.826112, 46.623093, 'https://maps.google.com/?q=24.826112,46.623093', 'ChIJe0c7fX4LLz4R9jN8sQYf9f8', GETUTCDATE(), GETUTCDATE()),
                ('22222222-2222-2222-2222-222222222222', 'demo-customer', 'Office', 'Riyadh', 'Al Olaya', 24.707707, 46.675296, 'https://maps.google.com/?q=24.707707,46.675296', 'ChIJw2h6G4YLLz4RW5h6qH9GM8w', GETUTCDATE(), GETUTCDATE())
                """);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccounts_CustomerReference",
                table: "CustomerAccounts",
                column: "CustomerReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccounts_NormalizedMobileNumber",
                table: "CustomerAccounts",
                column: "NormalizedMobileNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAuthSessions_AuthToken",
                table: "CustomerAuthSessions",
                column: "AuthToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAuthSessions_CustomerAccountId_ExpiresAtUtc",
                table: "CustomerAuthSessions",
                columns: new[] { "CustomerAccountId", "ExpiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM CustomerSavedLocations
                WHERE CustomerReference = 'demo-customer'
                """);

            migrationBuilder.DropTable(
                name: "CustomerAccounts");

            migrationBuilder.DropTable(
                name: "CustomerAuthSessions");
        }
    }
}
