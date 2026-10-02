using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PizzaApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDuplicateMenuItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "MenuItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 3,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 4,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 5,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 6,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 7,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 8,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 9,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 10,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 11,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 12,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 13,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 14,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 15,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 16,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 17,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 18,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 19,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 20,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 21,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 22,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 23,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 24,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 25,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 26,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 27,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 28,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 29,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 30,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 31,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 32,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 33,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 34,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 35,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 36,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 37,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 38,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 39,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 40,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 41,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 42,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 43,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 44,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 45,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 46,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 47,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 48,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 49,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 50,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 51,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 52,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 53,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 54,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 55,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 56,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 57,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 58,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 59,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 60,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 61,
                column: "IsHidden",
                value: false);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 62,
                column: "IsHidden",
                value: true);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 65,
                column: "IsHidden",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "MenuItems");

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Category", "Description", "MenuNumber", "Name", "Price", "RestaurantId" },
                values: new object[,]
                {
                    { 63, "Kebab", "Kebab, Pommes, Kebabsås", null, "Kebabrulle", 90m, 1 },
                    { 64, "Kebab", "Kyckling, Pommes, Kebabsås", null, "Kycklingrulle", 90m, 1 }
                });
        }
    }
}
