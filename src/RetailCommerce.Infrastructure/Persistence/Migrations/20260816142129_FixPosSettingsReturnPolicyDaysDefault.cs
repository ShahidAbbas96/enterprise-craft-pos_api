using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailCommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixPosSettingsReturnPolicyDaysDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ReturnPolicyDays",
                table: "PosSettings",
                type: "integer",
                nullable: false,
                defaultValue: 15,
                oldClrType: typeof(int),
                oldType: "integer");

            // The previous migration only set the SQL-level default going forward — any row
            // already in the table (i.e. every existing deployment's singleton PosSettings row)
            // was left at 0 by that column add. Backfill it to the intended 15-day default.
            migrationBuilder.Sql("UPDATE \"PosSettings\" SET \"ReturnPolicyDays\" = 15 WHERE \"ReturnPolicyDays\" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ReturnPolicyDays",
                table: "PosSettings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 15);
        }
    }
}
