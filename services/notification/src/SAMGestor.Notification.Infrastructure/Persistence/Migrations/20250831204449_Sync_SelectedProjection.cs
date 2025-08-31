using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Notification.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sync_SelectedProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_selreg_email_retreat",
                schema: "notification",
                table: "selected_registrations");

            migrationBuilder.RenameIndex(
                name: "ix_selreg_retreat",
                schema: "notification",
                table: "selected_registrations",
                newName: "IX_selected_registrations_RetreatId");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "notification",
                table: "selected_registrations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_selected_registrations_RetreatId",
                schema: "notification",
                table: "selected_registrations",
                newName: "ix_selreg_retreat");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "notification",
                table: "selected_registrations",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "ix_selreg_email_retreat",
                schema: "notification",
                table: "selected_registrations",
                columns: new[] { "Email", "RetreatId" });
        }
    }
}
