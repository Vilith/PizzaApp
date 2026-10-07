using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PizzaApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class SperringWeeklyMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SperringWeeks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    MenuJson = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SperringWeeks", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SperringWeeks");
        }
    }
}
