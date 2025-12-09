using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendify.DATA.Migrations
{
    /// <inheritdoc />
    public partial class leaverequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Reason",
                table: "Leaves",
                newName: "ReasonTitle");

            migrationBuilder.AddColumn<string>(
                name: "AdminResponse",
                table: "Leaves",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Detail",
                table: "Leaves",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminResponse",
                table: "Leaves");

            migrationBuilder.DropColumn(
                name: "Detail",
                table: "Leaves");

            migrationBuilder.RenameColumn(
                name: "ReasonTitle",
                table: "Leaves",
                newName: "Reason");
        }
    }
}
