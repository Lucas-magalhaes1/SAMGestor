using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdaptManualPaymentProofForService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_manual_payment_proofs_registration_id",
                schema: "core",
                table: "manual_payment_proofs");

            migrationBuilder.AlterColumn<Guid>(
                name: "registration_id",
                schema: "core",
                table: "manual_payment_proofs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "service_registration_id",
                schema: "core",
                table: "manual_payment_proofs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_manual_payment_proofs_registration_id",
                schema: "core",
                table: "manual_payment_proofs",
                column: "registration_id",
                unique: true,
                filter: "registration_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_manual_payment_proofs_service_registration_id",
                schema: "core",
                table: "manual_payment_proofs",
                column: "service_registration_id",
                unique: true,
                filter: "service_registration_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_manual_payment_proofs_service_registrations_service_registr~",
                schema: "core",
                table: "manual_payment_proofs",
                column: "service_registration_id",
                principalSchema: "core",
                principalTable: "service_registrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_manual_payment_proofs_service_registrations_service_registr~",
                schema: "core",
                table: "manual_payment_proofs");

            migrationBuilder.DropIndex(
                name: "ix_manual_payment_proofs_registration_id",
                schema: "core",
                table: "manual_payment_proofs");

            migrationBuilder.DropIndex(
                name: "ix_manual_payment_proofs_service_registration_id",
                schema: "core",
                table: "manual_payment_proofs");

            migrationBuilder.DropColumn(
                name: "service_registration_id",
                schema: "core",
                table: "manual_payment_proofs");

            migrationBuilder.AlterColumn<Guid>(
                name: "registration_id",
                schema: "core",
                table: "manual_payment_proofs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_manual_payment_proofs_registration_id",
                schema: "core",
                table: "manual_payment_proofs",
                column: "registration_id",
                unique: true);
        }
    }
}
