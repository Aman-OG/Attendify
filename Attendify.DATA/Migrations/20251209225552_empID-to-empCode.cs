using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Attendify.DATA.Migrations
{
    /// <inheritdoc />
    public partial class empIDtoempCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendance_Employees_EmployeeID",
                table: "Attendance");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeRequests_Employees_EmployeeID",
                table: "EmployeeRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Leaves_Employees_EmployeeID",
                table: "Leaves");

            migrationBuilder.DropIndex(
                name: "IX_Leaves_EmployeeID",
                table: "Leaves");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Employees",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeRequests_EmployeeID",
                table: "EmployeeRequests");

            migrationBuilder.DropIndex(
                name: "IX_Attendance_EmployeeID",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "EmployeeID",
                table: "Leaves");

            migrationBuilder.DropColumn(
                name: "EmployeeID",
                table: "Attendance");

            migrationBuilder.AddColumn<string>(
                name: "EmpCode",
                table: "Leaves",
                type: "character varying(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeID",
                table: "Employees",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "EmpCode",
                table: "EmployeeRequests",
                type: "character varying(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmpCode",
                table: "Attendance",
                type: "character varying(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Attendance",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Employees",
                table: "Employees",
                column: "EmpCode");

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_EmpCode",
                table: "Leaves",
                column: "EmpCode");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRequests_EmpCode",
                table: "EmployeeRequests",
                column: "EmpCode");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_EmpCode",
                table: "Attendance",
                column: "EmpCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendance_Employees_EmpCode",
                table: "Attendance",
                column: "EmpCode",
                principalTable: "Employees",
                principalColumn: "EmpCode",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeRequests_Employees_EmpCode",
                table: "EmployeeRequests",
                column: "EmpCode",
                principalTable: "Employees",
                principalColumn: "EmpCode",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Leaves_Employees_EmpCode",
                table: "Leaves",
                column: "EmpCode",
                principalTable: "Employees",
                principalColumn: "EmpCode",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendance_Employees_EmpCode",
                table: "Attendance");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeRequests_Employees_EmpCode",
                table: "EmployeeRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Leaves_Employees_EmpCode",
                table: "Leaves");

            migrationBuilder.DropIndex(
                name: "IX_Leaves_EmpCode",
                table: "Leaves");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Employees",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeRequests_EmpCode",
                table: "EmployeeRequests");

            migrationBuilder.DropIndex(
                name: "IX_Attendance_EmpCode",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "EmpCode",
                table: "Leaves");

            migrationBuilder.DropColumn(
                name: "EmpCode",
                table: "EmployeeRequests");

            migrationBuilder.DropColumn(
                name: "EmpCode",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Attendance");

            migrationBuilder.AddColumn<int>(
                name: "EmployeeID",
                table: "Leaves",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeID",
                table: "Employees",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeID",
                table: "Attendance",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Employees",
                table: "Employees",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_EmployeeID",
                table: "Leaves",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRequests_EmployeeID",
                table: "EmployeeRequests",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_EmployeeID",
                table: "Attendance",
                column: "EmployeeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendance_Employees_EmployeeID",
                table: "Attendance",
                column: "EmployeeID",
                principalTable: "Employees",
                principalColumn: "EmployeeID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeRequests_Employees_EmployeeID",
                table: "EmployeeRequests",
                column: "EmployeeID",
                principalTable: "Employees",
                principalColumn: "EmployeeID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Leaves_Employees_EmployeeID",
                table: "Leaves",
                column: "EmployeeID",
                principalTable: "Employees",
                principalColumn: "EmployeeID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
