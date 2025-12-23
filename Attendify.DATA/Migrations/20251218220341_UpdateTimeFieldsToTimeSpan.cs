using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendify.DATA.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTimeFieldsToTimeSpan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Shifts\" ALTER COLUMN \"StartTime\" TYPE interval USING \"StartTime\"::interval;");
            migrationBuilder.Sql("ALTER TABLE \"Shifts\" ALTER COLUMN \"EndTime\" TYPE interval USING \"EndTime\"::interval;");
            migrationBuilder.Sql("ALTER TABLE \"AttendanceRules\" ALTER COLUMN \"StartTime\" TYPE interval USING \"StartTime\"::interval;");
            migrationBuilder.Sql("ALTER TABLE \"AttendanceRules\" ALTER COLUMN \"EndTime\" TYPE interval USING \"EndTime\"::interval;");

            // Still keep AlterColumn metadata if needed, but the RAW SQL handles the change.
            // Actually, just SQL is cleaner if we are sure about PG.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StartTime",
                table: "Shifts",
                type: "text",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "interval");

            migrationBuilder.AlterColumn<string>(
                name: "EndTime",
                table: "Shifts",
                type: "text",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "interval");

            migrationBuilder.AlterColumn<string>(
                name: "StartTime",
                table: "AttendanceRules",
                type: "text",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "interval");

            migrationBuilder.AlterColumn<string>(
                name: "EndTime",
                table: "AttendanceRules",
                type: "text",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "interval");
        }
    }
}
