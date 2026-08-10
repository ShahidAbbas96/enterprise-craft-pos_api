using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailCommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBarcodeSettingsAndMultiActiveBarcodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupersededAtUtc",
                table: "ProductBarcodes");

            migrationBuilder.RenameColumn(
                name: "IsCurrent",
                table: "ProductBarcodes",
                newName: "IsPrimary");

            migrationBuilder.RenameIndex(
                name: "IX_ProductBarcodes_ProductId_IsCurrent",
                table: "ProductBarcodes",
                newName: "IX_ProductBarcodes_ProductId_IsPrimary");

            // Every barcode assigned before this feature existed was, by definition, still
            // valid/scannable — default existing rows to active rather than the column's
            // otherwise-implied "retired" false.
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProductBarcodes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "BarcodeSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IncludeCompanyName = table.Column<bool>(type: "boolean", nullable: false),
                    IncludePrice = table.Column<bool>(type: "boolean", nullable: false),
                    LabelWidthInches = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    LabelHeightInches = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarcodeSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BarcodeSettings");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProductBarcodes");

            migrationBuilder.RenameColumn(
                name: "IsPrimary",
                table: "ProductBarcodes",
                newName: "IsCurrent");

            migrationBuilder.RenameIndex(
                name: "IX_ProductBarcodes_ProductId_IsPrimary",
                table: "ProductBarcodes",
                newName: "IX_ProductBarcodes_ProductId_IsCurrent");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SupersededAtUtc",
                table: "ProductBarcodes",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
