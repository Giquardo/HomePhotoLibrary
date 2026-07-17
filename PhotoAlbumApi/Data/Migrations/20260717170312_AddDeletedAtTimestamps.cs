using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_dev.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletedAtTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "Photos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "Albums",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Albums");
        }
    }
}
