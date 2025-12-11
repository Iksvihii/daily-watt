using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyWatt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameAddressToCityInEnedisCredential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Address",
                table: "EnedisCredentials",
                newName: "City");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "City",
                table: "EnedisCredentials",
                newName: "Address");
        }
    }
}
