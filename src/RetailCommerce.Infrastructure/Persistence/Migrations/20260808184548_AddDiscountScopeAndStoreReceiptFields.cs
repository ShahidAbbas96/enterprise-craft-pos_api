using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailCommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountScopeAndStoreReceiptFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Stores",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ntn",
                table: "Stores",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptFooterText",
                table: "Stores",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Strn",
                table: "Stores",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Discounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "Discounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "Discounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_DepartmentId",
                table: "Discounts",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_ProductId",
                table: "Discounts",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Discounts_Departments_DepartmentId",
                table: "Discounts",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Discounts_Products_ProductId",
                table: "Discounts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Discounts_Departments_DepartmentId",
                table: "Discounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Discounts_Products_ProductId",
                table: "Discounts");

            migrationBuilder.DropIndex(
                name: "IX_Discounts_DepartmentId",
                table: "Discounts");

            migrationBuilder.DropIndex(
                name: "IX_Discounts_ProductId",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "Ntn",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "ReceiptFooterText",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "Strn",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "Discounts");
        }
    }
}
