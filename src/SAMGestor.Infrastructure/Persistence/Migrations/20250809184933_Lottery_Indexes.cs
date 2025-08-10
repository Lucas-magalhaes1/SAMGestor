using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Lottery_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_registrations_cpf",
                table: "registrations");

            migrationBuilder.DropIndex(
                name: "IX_registrations_email",
                table: "registrations");

            migrationBuilder.RenameColumn(
                name: "RetreatId",
                table: "registrations",
                newName: "retreat_id");

            migrationBuilder.CreateIndex(
                name: "IX_registrations_cpf",
                table: "registrations",
                column: "cpf");

            migrationBuilder.CreateIndex(
                name: "IX_registrations_email",
                table: "registrations",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_registrations_retreat_id_cpf",
                table: "registrations",
                columns: new[] { "retreat_id", "cpf" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_registrations_retreat_id_email",
                table: "registrations",
                columns: new[] { "retreat_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_registrations_retreat_id_status_gender",
                table: "registrations",
                columns: new[] { "retreat_id", "status", "gender" });

            migrationBuilder.AddForeignKey(
                name: "FK_registrations_retreats_retreat_id",
                table: "registrations",
                column: "retreat_id",
                principalTable: "retreats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_registrations_retreats_retreat_id",
                table: "registrations");

            migrationBuilder.DropIndex(
                name: "IX_registrations_cpf",
                table: "registrations");

            migrationBuilder.DropIndex(
                name: "IX_registrations_email",
                table: "registrations");

            migrationBuilder.DropIndex(
                name: "IX_registrations_retreat_id_cpf",
                table: "registrations");

            migrationBuilder.DropIndex(
                name: "IX_registrations_retreat_id_email",
                table: "registrations");

            migrationBuilder.DropIndex(
                name: "IX_registrations_retreat_id_status_gender",
                table: "registrations");

            migrationBuilder.RenameColumn(
                name: "retreat_id",
                table: "registrations",
                newName: "RetreatId");

            migrationBuilder.CreateIndex(
                name: "IX_registrations_cpf",
                table: "registrations",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_registrations_email",
                table: "registrations",
                column: "email",
                unique: true);
        }
    }
}
