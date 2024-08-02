using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esercitazione.Migrations
{
    /// <inheritdoc />
    public partial class OrderInCart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InCart",
                table: "Orders",
                newName: "IsInCart");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsInCart",
                table: "Orders",
                newName: "InCart");
        }
    }
}
