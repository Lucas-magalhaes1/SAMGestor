using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfilePhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "photo_content_type",
                schema: "core",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "photo_size_bytes",
                schema: "core",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "photo_storage_key",
                schema: "core",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "photo_uploaded_at",
                schema: "core",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_photo_storage_key",
                schema: "core",
                table: "users",
                column: "photo_storage_key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_photo_storage_key",
                schema: "core",
                table: "users");

            migrationBuilder.DropColumn(
                name: "photo_content_type",
                schema: "core",
                table: "users");

            migrationBuilder.DropColumn(
                name: "photo_size_bytes",
                schema: "core",
                table: "users");

            migrationBuilder.DropColumn(
                name: "photo_storage_key",
                schema: "core",
                table: "users");

            migrationBuilder.DropColumn(
                name: "photo_uploaded_at",
                schema: "core",
                table: "users");
        }
    }
}
