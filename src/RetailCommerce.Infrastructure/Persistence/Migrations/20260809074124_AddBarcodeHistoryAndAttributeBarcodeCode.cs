using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailCommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBarcodeHistoryAndAttributeBarcodeCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BarcodeCode",
                table: "ProductAttributeOptions",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductBarcodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    SupersededAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBarcodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductBarcodes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductBarcodes_Code",
                table: "ProductBarcodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductBarcodes_ProductId_IsCurrent",
                table: "ProductBarcodes",
                columns: new[] { "ProductId", "IsCurrent" });

            // Sensible default 2-letter barcode short-codes for the Color options seeded at
            // runtime this session (their auto-derived first-2-letters would collide, e.g.
            // BLACK/BLUE/BROWN all -> "BL") — admins can still override per-option later.
            migrationBuilder.Sql(@"
                UPDATE ""ProductAttributeOptions"" o
                SET ""BarcodeCode"" = v.code
                FROM (VALUES
                    ('BLACK', 'BK'), ('BLUE', 'BU'), ('BROWN', 'BR'), ('GOLDEN', 'GD'),
                    ('LIGHT_PINK', 'LP'), ('MAROON', 'MR'), ('METAL', 'MT'), ('MINT_GREEN', 'MG'),
                    ('MULTY', 'ML'), ('NAVY', 'NV'), ('RED', 'RD'), ('SILVER', 'SV'),
                    ('WHITE', 'WH'), ('YELLOW', 'YL')
                ) AS v(option_code, code)
                WHERE o.""Code"" = v.option_code
                  AND EXISTS (
                      SELECT 1 FROM ""ProductAttributeTypes"" t
                      WHERE t.""Id"" = o.""ProductAttributeTypeId"" AND t.""Code"" = 'COLOR'
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductBarcodes");

            migrationBuilder.DropColumn(
                name: "BarcodeCode",
                table: "ProductAttributeOptions");
        }
    }
}
