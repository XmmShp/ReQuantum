using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReQuantum.MAUI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Iteration260313 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TodoId",
                table: "CalendarTodos");

            migrationBuilder.DropColumn(
                name: "NoteId",
                table: "CalendarNotes");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "CalendarEvents");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "StorageEntries",
                newName: "Key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Key",
                table: "StorageEntries",
                newName: "Id");

            migrationBuilder.AddColumn<int>(
                name: "TodoId",
                table: "CalendarTodos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NoteId",
                table: "CalendarNotes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "CalendarEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
