using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PizzaApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class UniqueAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AliasKey",
                table: "UserProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LoginEmail",
                table: "UserProfiles",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "UserProfiles" SET "AliasKey" = upper(btrim("DisplayName"));
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM "UserProfiles" GROUP BY "AliasKey" HAVING count(*) > 1) THEN
                        RAISE EXCEPTION 'Duplicate nick/alias: resolve duplicate DisplayName values in UserProfiles before applying UniqueAliases. No accounts have been renamed.';
                    END IF;
                    IF to_regclass('auth.users') IS NOT NULL THEN
                        UPDATE "UserProfiles" p SET "LoginEmail" = u.email
                        FROM auth.users u WHERE p."UserId" = u.id AND u.email_confirmed_at IS NOT NULL;
                    END IF;
                END $$;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_AliasKey",
                table: "UserProfiles",
                column: "AliasKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_AliasKey",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "AliasKey",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "LoginEmail",
                table: "UserProfiles");
        }
    }
}
