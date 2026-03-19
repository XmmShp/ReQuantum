using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReQuantum.MAUI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "CalendarTodos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSource",
                table: "CalendarTodos",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "CalendarTodos");

            migrationBuilder.DropColumn(
                name: "ExternalSource",
                table: "CalendarTodos");
        }
    }
}
