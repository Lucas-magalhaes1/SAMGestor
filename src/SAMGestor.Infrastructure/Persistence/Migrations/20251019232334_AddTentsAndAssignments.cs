using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTentsAndAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "category",
                schema: "core",
                table: "tents",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "core",
                table: "tents",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_locked",
                schema: "core",
                table: "tents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                schema: "core",
                table: "tents",
                type: "character varying(280)",
                maxLength: 280,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "tents_locked",
                schema: "core",
                table: "retreats",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "tents_version",
                schema: "core",
                table: "retreats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "tent_assignments",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: true),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tent_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tent_assignments_registrations_registration_id",
                        column: x => x.registration_id,
                        principalSchema: "core",
                        principalTable: "registrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tent_assignments_tents_tent_id",
                        column: x => x.tent_id,
                        principalSchema: "core",
                        principalTable: "tents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tents_retreat_id",
                schema: "core",
                table: "tents",
                column: "retreat_id");
            
            migrationBuilder.CreateIndex(
                name: "ux_tents_retreat_category_number",
                schema: "core",
                table: "tents",
                columns: new[] { "retreat_id", "category", "number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_tents_capacity_positive",
                schema: "core",
                table: "tents",
                sql: "capacity > 0");

            migrationBuilder.CreateIndex(
                name: "IX_tent_assignments_tent_id",
                schema: "core",
                table: "tent_assignments",
                column: "tent_id");

            migrationBuilder.CreateIndex(
                name: "ux_tent_assignments_registration",
                schema: "core",
                table: "tent_assignments",
                column: "registration_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tent_assignments",
                schema: "core");

            migrationBuilder.DropIndex(
                name: "IX_tents_retreat_id",
                schema: "core",
                table: "tents");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tents_capacity_positive",
                schema: "core",
                table: "tents");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "core",
                table: "tents");

            migrationBuilder.DropColumn(
                name: "is_locked",
                schema: "core",
                table: "tents");

            migrationBuilder.DropColumn(
                name: "notes",
                schema: "core",
                table: "tents");

            migrationBuilder.DropColumn(
                name: "tents_locked",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "tents_version",
                schema: "core",
                table: "retreats");

            migrationBuilder.AlterColumn<string>(
                name: "category",
                schema: "core",
                table: "tents",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);
        }
    }
}
