using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Families_GroupColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "group_channel",
                schema: "core",
                table: "families",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "group_created_at",
                schema: "core",
                table: "families",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "group_external_id",
                schema: "core",
                table: "families",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "group_last_notified_at",
                schema: "core",
                table: "families",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "group_link",
                schema: "core",
                table: "families",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "group_status",
                schema: "core",
                table: "families",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "group_version",
                schema: "core",
                table: "families",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_families_retreat_id_group_status",
                schema: "core",
                table: "families",
                columns: new[] { "retreat_id", "group_status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_families_retreat_id_group_status",
                schema: "core",
                table: "families");

            migrationBuilder.DropColumn(
                name: "group_channel",
                schema: "core",
                table: "families");

            migrationBuilder.DropColumn(
                name: "group_created_at",
                schema: "core",
                table: "families");

            migrationBuilder.DropColumn(
                name: "group_external_id",
                schema: "core",
                table: "families");

            migrationBuilder.DropColumn(
                name: "group_last_notified_at",
                schema: "core",
                table: "families");

            migrationBuilder.DropColumn(
                name: "group_link",
                schema: "core",
                table: "families");

            migrationBuilder.DropColumn(
                name: "group_status",
                schema: "core",
                table: "families");

            migrationBuilder.DropColumn(
                name: "group_version",
                schema: "core",
                table: "families");
        }
    }
}
