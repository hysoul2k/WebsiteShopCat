using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Huy_Final_0843.Migrations
{
    /// <inheritdoc />
    public partial class AddSignalRFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Thêm cột mới lưu Enum (Dạng số nguyên)
            migrationBuilder.AddColumn<int>(
                name: "StatusInt",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // 2. Chuyển đổi dữ liệu từ chuỗi sang số
            // 0: Pending, 1: Shipping, 2: Completed, 3: Cancelled
            migrationBuilder.Sql(@"
                UPDATE Orders SET StatusInt = 
                CASE 
                    WHEN Status LIKE N'%Chờ%' THEN 0
                    WHEN Status LIKE N'%Giao%' THEN 1
                    WHEN Status LIKE N'%Hoàn thành%' OR Status LIKE N'%Xong%' THEN 2
                    WHEN Status LIKE N'%Hủy%' THEN 3
                    ELSE 0 
                END");

            // 3. Xóa cột chuỗi cũ và đổi tên cột mới
            migrationBuilder.DropColumn(name: "Status", table: "Orders");
            migrationBuilder.RenameColumn(name: "StatusInt", table: "Orders", newName: "Status");

            // 4. Thêm cột CancellationReason
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Thêm cột mới lưu chữ
            migrationBuilder.AddColumn<string>(
                name: "StatusString",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Chờ xử lý");

            // 2. Chuyển đổi dữ liệu từ số sang chữ
            migrationBuilder.Sql(@"
                UPDATE Orders SET StatusString = 
                CASE 
                    WHEN Status = 0 THEN N'Chờ xử lý'
                    WHEN Status = 1 THEN N'Đang giao hàng'
                    WHEN Status = 2 THEN N'Hoàn thành'
                    WHEN Status = 3 THEN N'Đã hủy'
                    ELSE N'Chờ xử lý'
                END");

            // 3. Xóa cột số và đổi tên cột mới
            migrationBuilder.DropColumn(name: "Status", table: "Orders");
            migrationBuilder.RenameColumn(name: "StatusString", table: "Orders", newName: "Status");

            // 4. Xóa CancellationReason
            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Orders");
        }
    }
}
