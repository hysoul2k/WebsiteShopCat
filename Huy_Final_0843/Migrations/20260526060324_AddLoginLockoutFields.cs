using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Huy_Final_0843.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginLockoutFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPermanentlyLocked",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFailedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalLockCount",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsPermanentlyLocked",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastFailedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TotalLockCount",
                table: "AspNetUsers");
        }
    }
}
