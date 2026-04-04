using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belkhedma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorProviderJsonDocumentsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "JsonContent",
                table: "ProviderJsonDocuments",
                newName: "JsonData");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProviderJsonDocuments",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "JsonAttributes",
                table: "ProviderJsonDocuments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                table: "ProviderJsonDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceOfferId",
                table: "ProviderJsonDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE d
                SET d.ProviderId = p.Id
                FROM ProviderJsonDocuments AS d
                INNER JOIN Providers AS p ON p.Code = d.ProviderCode
                """);

            migrationBuilder.Sql(
                """
                UPDATE d
                SET d.ServiceOfferId = so.Id
                FROM ProviderJsonDocuments AS d
                OUTER APPLY (
                    SELECT TOP 1 s.Id
                    FROM ServiceOffers AS s
                    WHERE s.ProviderId = d.ProviderId
                    ORDER BY
                        CASE WHEN d.ServiceMode IS NOT NULL AND s.ServiceMode = d.ServiceMode THEN 0 ELSE 1 END,
                        s.UpdatedAtUtc DESC
                ) AS so
                WHERE d.ProviderId IS NOT NULL
                """);

            migrationBuilder.Sql(
                """
                DECLARE @fallbackProviderId uniqueidentifier = (SELECT TOP 1 Id FROM Providers ORDER BY CreatedAtUtc);
                DECLARE @fallbackServiceOfferId uniqueidentifier = (SELECT TOP 1 Id FROM ServiceOffers ORDER BY UpdatedAtUtc DESC);

                UPDATE ProviderJsonDocuments
                SET ProviderId = ISNULL(ProviderId, COALESCE(@fallbackProviderId, '00000000-0000-0000-0000-000000000000')),
                    ServiceOfferId = ISNULL(ServiceOfferId, COALESCE(@fallbackServiceOfferId, '00000000-0000-0000-0000-000000000000'))
                WHERE ProviderId IS NULL OR ServiceOfferId IS NULL
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProviderId",
                table: "ProviderJsonDocuments",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ServiceOfferId",
                table: "ProviderJsonDocuments",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "ProviderCode",
                table: "ProviderJsonDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderJsonDocuments_ProviderId_ServiceOfferId_IsActive",
                table: "ProviderJsonDocuments",
                columns: new[] { "ProviderId", "ServiceOfferId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProviderJsonDocuments_ProviderId_ServiceOfferId_IsActive",
                table: "ProviderJsonDocuments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProviderJsonDocuments");

            migrationBuilder.DropColumn(
                name: "JsonAttributes",
                table: "ProviderJsonDocuments");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "ProviderJsonDocuments");

            migrationBuilder.DropColumn(
                name: "ServiceOfferId",
                table: "ProviderJsonDocuments");

            migrationBuilder.RenameColumn(
                name: "JsonData",
                table: "ProviderJsonDocuments",
                newName: "JsonContent");

            migrationBuilder.AddColumn<string>(
                name: "ProviderCode",
                table: "ProviderJsonDocuments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
