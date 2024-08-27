using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend_dev.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Albums",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Albums", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Photos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AlbumId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    DateUploaded = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Extension = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    FilePath = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Url = table.Column<string>(type: "varchar(2083)", maxLength: 2083, nullable: false),
                    Hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Photos_Albums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Albums",
                columns: new[] { "Id", "Description", "Title" },
                values: new object[,]
                {
                    { 1, "Album for AI related photos", "AI" },
                    { 2, "Album for nature related photos", "Nature" },
                    { 3, "Album for travel related photos", "Travel" },
                    { 4, "Album for family related photos", "Family" },
                    { 5, "Album for food related photos", "Food" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_AlbumId",
                table: "Photos",
                column: "AlbumId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Photos");

            migrationBuilder.DropTable(
                name: "Albums");
        }
    }
}
