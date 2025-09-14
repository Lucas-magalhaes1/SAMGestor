using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Families_Lock_And_UniqueName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "families_locked",
                schema: "core",
                table: "retreats",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_retreats_name",
                schema: "core",
                table: "retreats",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_families_retreat_id_name",
                schema: "core",
                table: "families",
                columns: new[] { "retreat_id", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_retreats_name",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropIndex(
                name: "IX_families_retreat_id_name",
                schema: "core",
                table: "families");

            migrationBuilder.DropColumn(
                name: "families_locked",
                schema: "core",
                table: "retreats");
        }
    }
}
