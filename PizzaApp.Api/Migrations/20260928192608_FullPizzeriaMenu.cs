using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PizzaApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class FullPizzeriaMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "MenuItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MenuNumber",
                table: "MenuItems",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "MenuNumber", "Name", "Price" },
                values: new object[] { "Tomat, Ost", 1, "Margherita", 85m });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "MenuNumber", "Price" },
                values: new object[] { "Skinka", 2, 85m });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "MenuNumber", "Price" },
                values: new object[] { "Skinka, Champinjoner", 3, 85m });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Description", "MenuNumber", "Price" },
                values: new object[] { "Skinka, Ananas", 4, 85m });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Description", "MenuNumber" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Description", "MenuNumber" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Description", "MenuNumber" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Description", "MenuNumber" },
                values: new object[] { "", null });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Description", "MenuNumber", "Name", "Price", "RestaurantId" },
                values: new object[,]
                {
                    { 9, "Skinka (Inbakad)", 5, "Calzone", 85m, 1 },
                    { 10, "Tonfisk, Lök", 6, "Pescatore", 85m, 1 },
                    { 11, "Köttfärs, Vitlökssås", 7, "Caruso", 85m, 1 },
                    { 12, "Köttfärssås, Lök", 8, "Bolognese", 85m, 1 },
                    { 13, "Bacon, Lök", 9, "La Maffia", 85m, 1 },
                    { 14, "Salami", 10, "Cacciatore", 85m, 1 },
                    { 15, "Skinka, Räkor", 11, "Tomaso", 85m, 1 },
                    { 16, "Musslor, Räkor", 12, "Marinara", 85m, 1 },
                    { 17, "Skinka, Banan, Ananas, Curry", 13, "Africana", 90m, 1 },
                    { 18, "Skinka, Champinjoner, Räkor", 14, "Jamaica", 90m, 1 },
                    { 19, "Bacon, Champinjoner, Lök, Paprika", 15, "Mama Mia", 90m, 1 },
                    { 20, "Fläskfilé, Lök, Champinjoner, Bearnaisesås", 16, "Amore Mio", 90m, 1 },
                    { 21, "Fläskfilé, Lök, Champinjoner, Vitlökssås (Inbakad)", 17, "Ciao Ciao", 90m, 1 },
                    { 22, "Skinka, Ananas, Räkor", 18, "Prinsessa", 90m, 1 },
                    { 23, "Skinka, Bacon, Lök, Bearnaisesås", 19, "Papillon", 90m, 1 },
                    { 24, "Kyckling, Ananas, Jordnötter, Curry", 20, "Kycklingpizza", 90m, 1 },
                    { 25, "Champinjoner, Ananas, Paprika, Lök, Tomat, Sparris", 21, "Vegetariana", 90m, 1 },
                    { 26, "Kebabkött, Lök, Kebabsås", 22, "Kebabpizza", 90m, 1 },
                    { 27, "Champinjoner, Oxfilé, Gorgonzolaost", 23, "Gorgonzola", 90m, 1 },
                    { 28, "Skinka, Köttfärssås", 24, "Disco", 90m, 1 },
                    { 29, "Skinka, Kebabkött, Champinjoner, Bearnaisesås", 25, "Rolandpizza", 90m, 1 },
                    { 30, "Kebabkött, Lök, Paprika, Champinjoner, Kebabsås", 26, "Alexpizza", 90m, 1 },
                    { 31, "Kebabkött, Lök, Paprika, Stark kebabsås, Vitlökssås", 27, "Cyckelpizza", 90m, 1 },
                    { 32, "Oxfilé, Champinjoner, Sparris, Bearnaisesås", 28, "Husets pizza", 90m, 1 },
                    { 33, "Skinka, Musslor, Räkor, Champinjoner", 29, "Quatro Stagioni", 90m, 1 },
                    { 34, "Köttfärssås, Champinjoner, Lök, Tacosås, Vitlökssås", 30, "Mexicana", 90m, 1 },
                    { 35, "Skinka, Tacosås, Jalapeno, Lök, Vitlökssås", 31, "Azteka", 90m, 1 },
                    { 36, "Oxfilé, Champinjoner, Lök, Jalapeno, Tacosås, Vitlökssås", 32, "Acapulko", 90m, 1 },
                    { 37, "Kebabkött, Gurka, Tomat, Isbergssallad, Kebabsås, Lök", 33, "Kebabspecial", 90m, 1 },
                    { 38, "Kebabkött, Skinka, Pommes, Kebabsås", 34, "Vara Special", 90m, 1 },
                    { 39, "Kebabkött, Skinka, Ananas, Kebabsås", 35, "Tre Kronor", 90m, 1 },
                    { 40, "Fläskfilé, Champinjoner, Paprika, Lök, Bearnaisesås (Dubbel inbakad)", 36, "Flygande Tefat", 100m, 1 },
                    { 41, "Fläskfilé, Champinjoner, Paprika, Lök, Bearnaisesås (Halvt inbakad)", 37, "U-båt 1", 90m, 1 },
                    { 42, "Skinka, Kebabkött, Champinjoner, Kebabsås (Halvt inbakad)", 38, "U-båt 2", 90m, 1 },
                    { 43, "Kyckling, Räkor, Champinjoner, Lök, Paprika, Kebabsås", 39, "Kycklinggryta", 90m, 1 },
                    { 44, "Kebabkött, Skinka, Räkor, Ananas, Champinjoner, Kebabsås", 40, "Frank Special", 90m, 1 },
                    { 45, "Skinka, Kebabkött, Bacon, Champinjoner, Kebabsås", 41, "Kvänums Special", 90m, 1 },
                    { 46, "Skinka, Lök, Paprika, Champinjoner, Kebabkött, Kebabsås", 42, "Anderspizza", 90m, 1 },
                    { 47, "Skinka, Champinjoner, Köttfärs, Pommes, Kebabsås", 43, "Curuso Special", 90m, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DropColumn(
                name: "Description",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "MenuNumber",
                table: "MenuItems");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Name", "Price" },
                values: new object[] { "Margharita", 95m });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 2,
                column: "Price",
                value: 95m);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 3,
                column: "Price",
                value: 95m);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 4,
                column: "Price",
                value: 95m);
        }
    }
}
