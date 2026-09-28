using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PizzaApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class CompletePizzeriaMenuSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "MenuItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 1,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 2,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 3,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 4,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 5,
                column: "Category",
                value: "");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 6,
                column: "Category",
                value: "");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 7,
                column: "Category",
                value: "");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 8,
                column: "Category",
                value: "");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 9,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 10,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 11,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 12,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 13,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 14,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 15,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 16,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 17,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 18,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 19,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 20,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 21,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 22,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 23,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 24,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 25,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 26,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 27,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 28,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 29,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 30,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 31,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 32,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 33,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 34,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 35,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 36,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 37,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 38,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 39,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 40,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 41,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 42,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 43,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 44,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 45,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 46,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 47,
                column: "Category",
                value: "Pizzor");

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Category", "Description", "MenuNumber", "Name", "Price", "RestaurantId" },
                values: new object[,]
                {
                    { 48, "Sallader", "Skinka, Räkor, Ananas, Ost, Sallad, Gurka, Majs", null, "Amerikansk Sallad", 90m, 1 },
                    { 49, "Sallader", "Räkor, Ost, Ägg, Citron, Tomat, Sallad, Gurka, Majs", null, "Räksallad", 90m, 1 },
                    { 50, "Sallader", "Räkor, Musslor, Champinjoner, Majs, Citron, Tomat, Sallad, Gurka", null, "Västkustsallad", 90m, 1 },
                    { 51, "Sallader", "Kyckling, Ananas, Majs, Tomat, Sallad, Gurka", null, "Kycklingsallad", 90m, 1 },
                    { 52, "Sallader", "Tonfisk, Ost, Lök, Majs, Champinjoner, Tomat, Sallad, Gurka", null, "Tonfisksallad", 90m, 1 },
                    { 53, "Sallader", "Kebab, Lök, Majs, Champinjoner, Tomat, Sallad, Gurka", null, "Kebabsallad", 90m, 1 },
                    { 54, "Sallader", "Nötfärs, Majs, Lök, Ost, Ananas, Tomat, Gurka, Sallad, Tacosås", null, "Tacosallad", 90m, 1 },
                    { 55, "Sallader", "Sallad, Gurka, Ananas, Champinjoner, Paprika, Majs, Sparris", null, "Vegetarisk Sallad", 90m, 1 },
                    { 56, "Kebab", "Kebab eller kyckling, Sallad, Tomat, Gurka, Lök, Kebabsås", null, "Kebab m. Bröd", 90m, 1 },
                    { 57, "Kebab", "Pommes, Sallad, Gurka, Tomat, Lök, Kebabsås", null, "Kebab/Kyckling-Tallrik", 90m, 1 },
                    { 58, "Kebab", "Kebab eller kyckling, Sallad, Tomat, Gurka, Lök, Kebabsås", null, "Kebab/Kyckling-Rulle", 90m, 1 },
                    { 59, "Kebab", "Räkor, Sallad, Gurka, Tomat, Skinka, Ananas, Kebabsås", null, "Räkrulle", 90m, 1 },
                    { 60, "Kebab", "Skinka, Sallad, Gurka, Tomat, Ananas, Kebabsås", null, "Hawaiirulle", 90m, 1 },
                    { 61, "Kebab", "Nötfärs, Majs, Lök, Gurka, Tomat, Sallad, Tacosås, Jalapeno", null, "Tacorulle", 90m, 1 },
                    { 62, "Kebab", "Med potatismos, Kebabsås", null, "Kebabtallrik", 90m, 1 },
                    { 63, "Kebab", "Kebab, Pommes, Kebabsås", null, "Kebabrulle", 90m, 1 },
                    { 64, "Kebab", "Kyckling, Pommes, Kebabsås", null, "Kycklingrulle", 90m, 1 },
                    { 65, "Stekrätter", "Med bröd & Pommes", null, "Hamburgare 90gr", 80m, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DropColumn(
                name: "Category",
                table: "MenuItems");
        }
    }
}
