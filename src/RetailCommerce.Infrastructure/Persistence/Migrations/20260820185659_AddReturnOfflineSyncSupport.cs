using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailCommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnOfflineSyncSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientTransactionId",
                table: "Returns",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Returns_ClientTransactionId",
                table: "Returns",
                column: "ClientTransactionId",
                unique: true,
                filter: "\"ClientTransactionId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Returns_ClientTransactionId",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "ClientTransactionId",
                table: "Returns");
        }
    }
}
