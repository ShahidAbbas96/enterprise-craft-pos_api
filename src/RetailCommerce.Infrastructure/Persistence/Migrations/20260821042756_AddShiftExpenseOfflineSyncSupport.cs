using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailCommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftExpenseOfflineSyncSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientTransactionId",
                table: "Shifts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CloseClientTransactionId",
                table: "Shifts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientTransactionId",
                table: "Expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_ClientTransactionId",
                table: "Shifts",
                column: "ClientTransactionId",
                unique: true,
                filter: "\"ClientTransactionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ClientTransactionId",
                table: "Expenses",
                column: "ClientTransactionId",
                unique: true,
                filter: "\"ClientTransactionId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shifts_ClientTransactionId",
                table: "Shifts");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_ClientTransactionId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "ClientTransactionId",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "CloseClientTransactionId",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "ClientTransactionId",
                table: "Expenses");
        }
    }
}
