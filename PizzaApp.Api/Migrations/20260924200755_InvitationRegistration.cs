using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PizzaApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class InvitationRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRegisteredMember",
                table: "UserProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRegisteredMember",
                table: "UserProfiles");
        }
    }
}
