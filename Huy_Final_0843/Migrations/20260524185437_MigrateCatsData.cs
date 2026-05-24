using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Huy_Final_0843.Migrations
{
    /// <inheritdoc />
    public partial class MigrateCatsData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Copy actual cats (CategoryId = 1 = "Mèo Cảnh") from Products → Cats
            migrationBuilder.Sql(@"
                INSERT INTO Cats (Name, Price, Description, ImageUrl, Gender, StockQuantity)
                SELECT Name, Price, Description, ImageUrl, N'Không rõ', StockQuantity
                FROM Products
                WHERE CategoryId = 1
            ");

            migrationBuilder.Sql(@"
                DELETE FROM Products WHERE CategoryId = 1
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO Products (Name, Price, Description, ImageUrl, CategoryId, StockQuantity)
                SELECT Name, Price, Description, ImageUrl, 1, StockQuantity
                FROM Cats;

                DELETE FROM Cats;
            ");
        }
    }
}
