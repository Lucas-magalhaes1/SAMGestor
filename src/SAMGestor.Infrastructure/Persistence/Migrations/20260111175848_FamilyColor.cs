using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FamilyColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_families_capacity_positive",
                schema: "core",
                table: "families");

            migrationBuilder.AddColumn<bool>(
                name: "is_madrinha",
                schema: "core",
                table: "family_members",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_padrinho",
                schema: "core",
                table: "family_members",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "color_hex",
                schema: "core",
                table: "families",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "color_name",
                schema: "core",
                table: "families",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_family_members_family_id_is_madrinha",
                schema: "core",
                table: "family_members",
                columns: new[] { "family_id", "is_madrinha" });

            migrationBuilder.CreateIndex(
                name: "IX_family_members_family_id_is_padrinho",
                schema: "core",
                table: "family_members",
                columns: new[] { "family_id", "is_padrinho" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_family_members_godparent_exclusive",
                schema: "core",
                table: "family_members",
                sql: "NOT (is_padrinho = true AND is_madrinha = true)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_families_capacity_positive",
                schema: "core",
                table: "families",
                sql: "capacity >= 4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_family_members_family_id_is_madrinha",
                schema: "core",
                table: "family_members");

            migrationBuilder.DropIndex(
                name: "IX_family_members_family_id_is_padrinho",
                schema: "core",
                table: "family_members");

            migrationBuilder.DropCheckConstraint(
                name: "ck_family_members_godparent_exclusive",
                schema: "core",
                table: "family_members");

            migrationBuilder.DropCheckConstraint(
                name: "ck_families_capacity_positive",
                schema: "core",
                table: "families");

            migrationBuilder.DropColumn(
                name: "is_madrinha",
                schema: "core",
                table: "family_members");

            migrationBuilder.DropColumn(
                name: "is_padrinho",
                schema: "core",
                table: "family_members");

            migrationBuilder.DropColumn(
                name: "color_hex",
                schema: "core",
                table: "families");

            migrationBuilder.DropColumn(
                name: "color_name",
                schema: "core",
                table: "families");

            migrationBuilder.AddCheckConstraint(
                name: "ck_families_capacity_positive",
                schema: "core",
                table: "families",
                sql: "capacity > 0");
        }
    }
}
