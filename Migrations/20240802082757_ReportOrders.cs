using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esercitazione.Migrations
{
    /// <inheritdoc />
    public partial class ReportOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Completed",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Completed",
                table: "Orders");
        }
    }
}
