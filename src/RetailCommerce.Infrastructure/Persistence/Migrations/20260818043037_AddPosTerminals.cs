using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailCommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosTerminals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TerminalId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PosTerminals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosTerminals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosTerminals_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PosTerminalUsers",
                columns: table => new
                {
                    TerminalId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosTerminalUsers", x => new { x.TerminalId, x.UserId });
                    table.ForeignKey(
                        name: "FK_PosTerminalUsers_PosTerminals_TerminalId",
                        column: x => x.TerminalId,
                        principalTable: "PosTerminals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TerminalId",
                table: "Orders",
                column: "TerminalId");

            migrationBuilder.CreateIndex(
                name: "IX_PosTerminals_Code",
                table: "PosTerminals",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosTerminals_WarehouseId",
                table: "PosTerminals",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_PosTerminals_TerminalId",
                table: "Orders",
                column: "TerminalId",
                principalTable: "PosTerminals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_PosTerminals_TerminalId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "PosTerminalUsers");

            migrationBuilder.DropTable(
                name: "PosTerminals");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TerminalId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TerminalId",
                table: "Orders");
        }
    }
}
