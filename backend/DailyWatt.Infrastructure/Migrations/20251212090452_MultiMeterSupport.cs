using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyWatt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MultiMeterSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear existing data that would violate new foreign key constraints
            migrationBuilder.Sql("DELETE FROM \"Measurements\";");
            migrationBuilder.Sql("DELETE FROM \"WeatherDays\";");
            migrationBuilder.Sql("DELETE FROM \"ImportJobs\";");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WeatherDays",
                table: "WeatherDays");

            migrationBuilder.DropIndex(
                name: "IX_Measurements_UserId_TimestampUtc",
                table: "Measurements");

            migrationBuilder.DropColumn(
                name: "City",
                table: "EnedisCredentials");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "EnedisCredentials");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "EnedisCredentials");

            migrationBuilder.DropColumn(
                name: "MeterNumber",
                table: "EnedisCredentials");

            migrationBuilder.AddColumn<Guid>(
                name: "MeterId",
                table: "WeatherDays",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "MeterId",
                table: "Measurements",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "MeterId",
                table: "ImportJobs",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_WeatherDays",
                table: "WeatherDays",
                columns: new[] { "UserId", "MeterId", "Date" });

            migrationBuilder.CreateTable(
                name: "EnedisMeters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Prm = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Latitude = table.Column<double>(type: "REAL", nullable: true),
                    Longitude = table.Column<double>(type: "REAL", nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnedisMeters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnedisMeters_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeatherDays_MeterId",
                table: "WeatherDays",
                column: "MeterId");

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_MeterId",
                table: "Measurements",
                column: "MeterId");

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_UserId_MeterId_TimestampUtc",
                table: "Measurements",
                columns: new[] { "UserId", "MeterId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_MeterId",
                table: "ImportJobs",
                column: "MeterId");

            migrationBuilder.CreateIndex(
                name: "IX_EnedisMeters_UserId_Prm",
                table: "EnedisMeters",
                columns: new[] { "UserId", "Prm" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportJobs_EnedisMeters_MeterId",
                table: "ImportJobs",
                column: "MeterId",
                principalTable: "EnedisMeters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Measurements_EnedisMeters_MeterId",
                table: "Measurements",
                column: "MeterId",
                principalTable: "EnedisMeters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WeatherDays_EnedisMeters_MeterId",
                table: "WeatherDays",
                column: "MeterId",
                principalTable: "EnedisMeters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImportJobs_EnedisMeters_MeterId",
                table: "ImportJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Measurements_EnedisMeters_MeterId",
                table: "Measurements");

            migrationBuilder.DropForeignKey(
                name: "FK_WeatherDays_EnedisMeters_MeterId",
                table: "WeatherDays");

            migrationBuilder.DropTable(
                name: "EnedisMeters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WeatherDays",
                table: "WeatherDays");

            migrationBuilder.DropIndex(
                name: "IX_WeatherDays_MeterId",
                table: "WeatherDays");

            migrationBuilder.DropIndex(
                name: "IX_Measurements_MeterId",
                table: "Measurements");

            migrationBuilder.DropIndex(
                name: "IX_Measurements_UserId_MeterId_TimestampUtc",
                table: "Measurements");

            migrationBuilder.DropIndex(
                name: "IX_ImportJobs_MeterId",
                table: "ImportJobs");

            migrationBuilder.DropColumn(
                name: "MeterId",
                table: "WeatherDays");

            migrationBuilder.DropColumn(
                name: "MeterId",
                table: "Measurements");

            migrationBuilder.DropColumn(
                name: "MeterId",
                table: "ImportJobs");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "EnedisCredentials",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "EnedisCredentials",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "EnedisCredentials",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeterNumber",
                table: "EnedisCredentials",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WeatherDays",
                table: "WeatherDays",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_UserId_TimestampUtc",
                table: "Measurements",
                columns: new[] { "UserId", "TimestampUtc" });
        }
    }
}
