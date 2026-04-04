using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belkhedma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderConfigurationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiBaseUrl",
                table: "Providers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookingEmail",
                table: "Providers",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommunicationWays",
                table: "Providers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ContractDraftExpirationHours",
                table: "Providers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ContractMode",
                table: "Providers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IntegrationWays",
                table: "Providers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OperationsEmail",
                table: "Providers",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentCollectionMode",
                table: "Providers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PricingExpirationHours",
                table: "Providers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequirePaymentBeforeSubmission",
                table: "Providers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SessionExpirationHours",
                table: "Providers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SettingsJson",
                table: "Providers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Providers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "Providers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiBaseUrl",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "BookingEmail",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "CommunicationWays",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "ContractDraftExpirationHours",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "ContractMode",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "IntegrationWays",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "OperationsEmail",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "PaymentCollectionMode",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "PricingExpirationHours",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "RequirePaymentBeforeSubmission",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "SessionExpirationHours",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "SettingsJson",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "Providers");
        }
    }
}
