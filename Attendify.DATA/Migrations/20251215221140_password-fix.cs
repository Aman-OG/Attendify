using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendify.DATA.Migrations
{
    /// <inheritdoc />
    public partial class passwordfix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordChange",
                table: "Employees",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPasswordChange",
                table: "Employees");
        }
    }
}
