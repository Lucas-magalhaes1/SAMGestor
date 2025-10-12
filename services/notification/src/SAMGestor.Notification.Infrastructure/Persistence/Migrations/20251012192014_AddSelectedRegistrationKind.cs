using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Notification.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedRegistrationKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                schema: "notification",
                table: "selected_registrations",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                schema: "notification",
                table: "selected_registrations");
        }
    }
}
