using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Families_JoinFamilyMember_V1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_registrations_families_FamilyId",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropIndex(
                name: "IX_registrations_FamilyId",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "godfather_count",
                schema: "core",
                table: "families");

            migrationBuilder.DropColumn(
                name: "godmother_count",
                schema: "core",
                table: "families");

            migrationBuilder.RenameColumn(
                name: "member_limit",
                schema: "core",
                table: "families",
                newName: "capacity");

            migrationBuilder.AddColumn<int>(
                name: "families_version",
                schema: "core",
                table: "retreats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "family_members",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    retreat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_family_members", x => x.Id);
                    table.CheckConstraint("ck_family_members_position_nonneg", "position >= 0");
                    table.ForeignKey(
                        name: "FK_family_members_families_family_id",
                        column: x => x.family_id,
                        principalSchema: "core",
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_family_members_registrations_registration_id",
                        column: x => x.registration_id,
                        principalSchema: "core",
                        principalTable: "registrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_families_retreat_id",
                schema: "core",
                table: "families",
                column: "retreat_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_families_capacity_positive",
                schema: "core",
                table: "families",
                sql: "capacity > 0");

            migrationBuilder.CreateIndex(
                name: "IX_family_members_family_id_position",
                schema: "core",
                table: "family_members",
                columns: new[] { "family_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_family_members_family_id_registration_id",
                schema: "core",
                table: "family_members",
                columns: new[] { "family_id", "registration_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_family_members_registration_id",
                schema: "core",
                table: "family_members",
                column: "registration_id");

            migrationBuilder.CreateIndex(
                name: "IX_family_members_retreat_id_family_id",
                schema: "core",
                table: "family_members",
                columns: new[] { "retreat_id", "family_id" });

            migrationBuilder.CreateIndex(
                name: "IX_family_members_retreat_id_registration_id",
                schema: "core",
                table: "family_members",
                columns: new[] { "retreat_id", "registration_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_families_retreats_retreat_id",
                schema: "core",
                table: "families",
                column: "retreat_id",
                principalSchema: "core",
                principalTable: "retreats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_families_retreats_retreat_id",
                schema: "core",
                table: "families");

            migrationBuilder.DropTable(
                name: "family_members",
                schema: "core");

            migrationBuilder.DropIndex(
                name: "IX_families_retreat_id",
                schema: "core",
                table: "families");

            migrationBuilder.DropCheckConstraint(
                name: "ck_families_capacity_positive",
                schema: "core",
                table: "families");

            migrationBuilder.DropColumn(
                name: "families_version",
                schema: "core",
                table: "retreats");

            migrationBuilder.RenameColumn(
                name: "capacity",
                schema: "core",
                table: "families",
                newName: "member_limit");

            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId",
                schema: "core",
                table: "registrations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "godfather_count",
                schema: "core",
                table: "families",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "godmother_count",
                schema: "core",
                table: "families",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_registrations_FamilyId",
                schema: "core",
                table: "registrations",
                column: "FamilyId");

            migrationBuilder.AddForeignKey(
                name: "FK_registrations_families_FamilyId",
                schema: "core",
                table: "registrations",
                column: "FamilyId",
                principalSchema: "core",
                principalTable: "families",
                principalColumn: "Id");
        }
    }
}
