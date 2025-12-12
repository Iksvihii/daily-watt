using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyWatt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFilePathToImportJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "ImportJobs",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "ImportJobs");
        }
    }
}
