using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailCommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceAttributeVisibilityWithFieldPicker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowOnPos",
                table: "ProductAttributeTypes");

            migrationBuilder.DropColumn(
                name: "ShowProductAttributes",
                table: "PosSettings");

            migrationBuilder.AddColumn<string>(
                name: "VisibleProductFields",
                table: "PosSettings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "sku,barcode,price,totalStock");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VisibleProductFields",
                table: "PosSettings");

            migrationBuilder.AddColumn<bool>(
                name: "ShowOnPos",
                table: "ProductAttributeTypes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowProductAttributes",
                table: "PosSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }
    }
}
