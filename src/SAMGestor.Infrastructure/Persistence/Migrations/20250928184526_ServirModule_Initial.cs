using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ServirModule_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "service_locked",
                schema: "core",
                table: "retreats",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "service_spaces_version",
                schema: "core",
                table: "retreats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "service_spaces",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RetreatId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    MinPeople = table.Column<int>(type: "integer", nullable: false),
                    MaxPeople = table.Column<int>(type: "integer", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_spaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "service_registrations",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    email = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    gender = table.Column<string>(type: "text", nullable: false),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    region = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    photo_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    retreat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    preferred_space_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    registration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_registrations_retreats_retreat_id",
                        column: x => x.retreat_id,
                        principalSchema: "core",
                        principalTable: "retreats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_registrations_service_spaces_preferred_space_id",
                        column: x => x.preferred_space_id,
                        principalSchema: "core",
                        principalTable: "service_spaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_assignments",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_space_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AssignedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_assignments_service_registrations_service_registrat~",
                        column: x => x.service_registration_id,
                        principalSchema: "core",
                        principalTable: "service_registrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_assignments_service_spaces_service_space_id",
                        column: x => x.service_space_id,
                        principalSchema: "core",
                        principalTable: "service_spaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_registration_payments",
                schema: "core",
                columns: table => new
                {
                    ServiceRegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_registration_payments", x => new { x.ServiceRegistrationId, x.PaymentId });
                    table.ForeignKey(
                        name: "FK_service_registration_payments_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "core",
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_registration_payments_service_registrations_Service~",
                        column: x => x.ServiceRegistrationId,
                        principalSchema: "core",
                        principalTable: "service_registrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_service_assignments_service_registration_id",
                schema: "core",
                table: "service_assignments",
                column: "service_registration_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_assignments_service_space_id_role",
                schema: "core",
                table: "service_assignments",
                columns: new[] { "service_space_id", "role" });

            migrationBuilder.CreateIndex(
                name: "UX_service_assignments_one_vice_per_space",
                schema: "core",
                table: "service_assignments",
                column: "service_space_id",
                unique: true,
                filter: "role = 2");

            migrationBuilder.CreateIndex(
                name: "IX_service_registration_payments_PaymentId",
                schema: "core",
                table: "service_registration_payments",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_registrations_cpf",
                schema: "core",
                table: "service_registrations",
                column: "cpf");

            migrationBuilder.CreateIndex(
                name: "IX_service_registrations_email",
                schema: "core",
                table: "service_registrations",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_service_registrations_preferred_space_id",
                schema: "core",
                table: "service_registrations",
                column: "preferred_space_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_registrations_retreat_id_cpf",
                schema: "core",
                table: "service_registrations",
                columns: new[] { "retreat_id", "cpf" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_registrations_retreat_id_email",
                schema: "core",
                table: "service_registrations",
                columns: new[] { "retreat_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_registrations_retreat_id_status",
                schema: "core",
                table: "service_registrations",
                columns: new[] { "retreat_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_service_spaces_RetreatId_IsActive",
                schema: "core",
                table: "service_spaces",
                columns: new[] { "RetreatId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_service_spaces_RetreatId_Name",
                schema: "core",
                table: "service_spaces",
                columns: new[] { "RetreatId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_assignments",
                schema: "core");

            migrationBuilder.DropTable(
                name: "service_registration_payments",
                schema: "core");

            migrationBuilder.DropTable(
                name: "service_registrations",
                schema: "core");

            migrationBuilder.DropTable(
                name: "service_spaces",
                schema: "core");

            migrationBuilder.DropColumn(
                name: "service_locked",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "service_spaces_version",
                schema: "core",
                table: "retreats");
        }
    }
}
