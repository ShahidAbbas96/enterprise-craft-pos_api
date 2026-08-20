using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailCommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOfflineSyncSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CapturedOffline",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientTransactionId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SyncLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TerminalId = table.Column<Guid>(type: "uuid", nullable: true),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClientTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ClientTransactionId",
                table: "Orders",
                column: "ClientTransactionId",
                unique: true,
                filter: "\"ClientTransactionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_OccurredAtUtc",
                table: "SyncLogs",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_TerminalId",
                table: "SyncLogs",
                column: "TerminalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncLogs");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ClientTransactionId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CapturedOffline",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ClientTransactionId",
                table: "Orders");
        }
    }
}
