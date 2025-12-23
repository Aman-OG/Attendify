using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendify.DATA.Migrations
{
    /// <inheritdoc />
    public partial class AllowNullableEmpCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "EmpCode",
                table: "EmployeeRequests",
                type: "character varying(20)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "EmpCode",
                table: "EmployeeRequests",
                type: "character varying(20)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldNullable: true);
        }
    }
}
