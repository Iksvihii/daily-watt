using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyWatt.Infrastructure.Migrations
{
  /// <inheritdoc />
  public partial class AddWeatherDayCache : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "WeatherDays",
          columns: table => new
          {
            UserId = table.Column<Guid>(type: "TEXT", nullable: false),
            Date = table.Column<DateTime>(type: "TEXT", nullable: false),
            TempAvg = table.Column<double>(type: "REAL", nullable: false),
            TempMin = table.Column<double>(type: "REAL", nullable: false),
            TempMax = table.Column<double>(type: "REAL", nullable: false),
            Source = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
            Latitude = table.Column<double>(type: "REAL", nullable: false),
            Longitude = table.Column<double>(type: "REAL", nullable: false),
            CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_WeatherDays", x => new { x.UserId, x.Date });
            table.ForeignKey(
                      name: "FK_WeatherDays_AspNetUsers_UserId",
                      column: x => x.UserId,
                      principalTable: "AspNetUsers",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "WeatherDays");
    }
  }
}