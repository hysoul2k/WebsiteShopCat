using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Huy_Final_0843.Migrations
{
    /// <inheritdoc />
    public partial class CatReplaceStockWithAge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StockQuantity",
                table: "Cats",
                newName: "Age");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Age",
                table: "Cats",
                newName: "StockQuantity");
        }
    }
}
