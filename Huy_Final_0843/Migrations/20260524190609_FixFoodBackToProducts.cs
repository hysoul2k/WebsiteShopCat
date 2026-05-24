using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Huy_Final_0843.Migrations
{
    /// <inheritdoc />
    public partial class FixFoodBackToProducts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // All real cat breeds start with "Mèo"; food items (Hạt, Pate, ...) do not.
            migrationBuilder.Sql(@"
                INSERT INTO Products (Name, Price, Description, ImageUrl, CategoryId, StockQuantity)
                SELECT Name, Price, Description, ImageUrl, 2, StockQuantity
                FROM Cats
                WHERE Name NOT LIKE N'Mèo%'
            ");

            migrationBuilder.Sql(@"
                DELETE FROM Cats WHERE Name NOT LIKE N'Mèo%'
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO Cats (Name, Price, Description, ImageUrl, Gender, StockQuantity)
                SELECT Name, Price, Description, ImageUrl, N'Không rõ', StockQuantity
                FROM Products WHERE CategoryId = 2;

                DELETE FROM Products WHERE CategoryId = 2;
            ");
        }
    }
}
