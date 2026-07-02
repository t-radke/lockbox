using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lockbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameFileRecordTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_FileRecord",
                table: "FileRecord");

            migrationBuilder.RenameTable(
                name: "FileRecord",
                newName: "FileRecords");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FileRecords",
                table: "FileRecords",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_FileRecords",
                table: "FileRecords");

            migrationBuilder.RenameTable(
                name: "FileRecords",
                newName: "FileRecord");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FileRecord",
                table: "FileRecord",
                column: "Id");
        }
    }
}
