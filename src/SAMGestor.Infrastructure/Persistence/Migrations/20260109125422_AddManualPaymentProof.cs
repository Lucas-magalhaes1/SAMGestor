using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManualPaymentProof : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "manual_payment_proofs",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    method = table.Column<string>(type: "text", nullable: false),
                    payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    proof_storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    proof_content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    proof_size_bytes = table.Column<int>(type: "integer", nullable: true),
                    proof_uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    registered_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manual_payment_proofs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_manual_payment_proofs_registrations_registration_id",
                        column: x => x.registration_id,
                        principalSchema: "core",
                        principalTable: "registrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_manual_payment_proofs_registered_at",
                schema: "core",
                table: "manual_payment_proofs",
                column: "registered_at");

            migrationBuilder.CreateIndex(
                name: "ix_manual_payment_proofs_registration_id",
                schema: "core",
                table: "manual_payment_proofs",
                column: "registration_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "manual_payment_proofs",
                schema: "core");
        }
    }
}
