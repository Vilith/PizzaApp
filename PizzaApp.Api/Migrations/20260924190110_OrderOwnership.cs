using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PizzaApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class OrderOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            // The app uses its .NET API, never PostgREST. Public Supabase keys
            // must not provide a second path around ownership and admin checks.
            migrationBuilder.Sql("""
                ALTER TABLE public."Orders" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public."CompletedOrderDays" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public."Restaurants" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public."MenuItems" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE public."Orders", public."CompletedOrderDays", public."Restaurants", public."MenuItems" FROM PUBLIC;
                DO $permissions$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'anon') THEN
                        REVOKE ALL ON TABLE public."Orders", public."CompletedOrderDays", public."Restaurants", public."MenuItems" FROM anon;
                    END IF;
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'authenticated') THEN
                        REVOKE ALL ON TABLE public."Orders", public."CompletedOrderDays", public."Restaurants", public."MenuItems" FROM authenticated;
                    END IF;
                END $permissions$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately retain database access restrictions on downgrade.
            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Orders");
        }
    }
}
