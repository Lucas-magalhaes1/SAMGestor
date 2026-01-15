using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenUsedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "used_at",
                schema: "core",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_used_at",
                schema: "core",
                table: "refresh_tokens",
                column: "used_at");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_active",
                schema: "core",
                table: "refresh_tokens",
                columns: new[] { "user_id", "revoked_at", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_used_at",
                schema: "core",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_user_active",
                schema: "core",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "used_at",
                schema: "core",
                table: "refresh_tokens");
        }
    }
}
