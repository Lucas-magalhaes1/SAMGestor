using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Core_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "core");

            migrationBuilder.CreateTable(
                name: "blocked_cpfs",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blocked_cpfs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "change_logs",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_change_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "families",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    godfather_count = table.Column<int>(type: "integer", nullable: false),
                    godmother_count = table.Column<int>(type: "integer", nullable: false),
                    retreat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_limit = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_families", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "message_templates",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    has_placeholders = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "messages_sent",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages_sent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    method = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "region_configs",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    target_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    observation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    retreat_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_region_configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "retreats",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    edition = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    theme = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    male_slots = table.Column<int>(type: "integer", nullable: false),
                    female_slots = table.Column<int>(type: "integer", nullable: false),
                    registration_start = table.Column<DateOnly>(type: "date", nullable: false),
                    registration_end = table.Column<DateOnly>(type: "date", nullable: false),
                    fee_fazer_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    fee_fazer_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    fee_servir_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    fee_servir_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    west_region_pct = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    other_regions_pct = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    contemplation_closed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retreats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    member_limit = table.Column<int>(type: "integer", nullable: false),
                    retreat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    min_members = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tents",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    retreat_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    email = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "waiting_list_items",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    retreat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_waiting_list_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "registrations",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    email = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    gender = table.Column<string>(type: "text", nullable: false),
                    city = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    photo_url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    participation_category = table.Column<string>(type: "text", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    region = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: true),
                    TentId = table.Column<Guid>(type: "uuid", nullable: true),
                    retreat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_retreat = table.Column<bool>(type: "boolean", nullable: false),
                    registration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_registrations_families_FamilyId",
                        column: x => x.FamilyId,
                        principalSchema: "core",
                        principalTable: "families",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_registrations_retreats_retreat_id",
                        column: x => x.retreat_id,
                        principalSchema: "core",
                        principalTable: "retreats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "team_members",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_members_teams_team_id",
                        column: x => x.team_id,
                        principalSchema: "core",
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_blocked_cpfs_cpf",
                schema: "core",
                table: "blocked_cpfs",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_region_configs_retreat_id_name",
                schema: "core",
                table: "region_configs",
                columns: new[] { "retreat_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_registrations_cpf",
                schema: "core",
                table: "registrations",
                column: "cpf");

            migrationBuilder.CreateIndex(
                name: "IX_registrations_email",
                schema: "core",
                table: "registrations",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_registrations_FamilyId",
                schema: "core",
                table: "registrations",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_registrations_retreat_id_cpf",
                schema: "core",
                table: "registrations",
                columns: new[] { "retreat_id", "cpf" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_registrations_retreat_id_email",
                schema: "core",
                table: "registrations",
                columns: new[] { "retreat_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_registrations_retreat_id_status_gender",
                schema: "core",
                table: "registrations",
                columns: new[] { "retreat_id", "status", "gender" });

            migrationBuilder.CreateIndex(
                name: "IX_team_members_team_id_registration_id",
                schema: "core",
                table: "team_members",
                columns: new[] { "team_id", "registration_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                schema: "core",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_waiting_list_items_registration_id",
                schema: "core",
                table: "waiting_list_items",
                column: "registration_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_waiting_list_items_retreat_id_position",
                schema: "core",
                table: "waiting_list_items",
                columns: new[] { "retreat_id", "position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blocked_cpfs",
                schema: "core");

            migrationBuilder.DropTable(
                name: "change_logs",
                schema: "core");

            migrationBuilder.DropTable(
                name: "message_templates",
                schema: "core");

            migrationBuilder.DropTable(
                name: "messages_sent",
                schema: "core");

            migrationBuilder.DropTable(
                name: "payments",
                schema: "core");

            migrationBuilder.DropTable(
                name: "region_configs",
                schema: "core");

            migrationBuilder.DropTable(
                name: "registrations",
                schema: "core");

            migrationBuilder.DropTable(
                name: "team_members",
                schema: "core");

            migrationBuilder.DropTable(
                name: "tents",
                schema: "core");

            migrationBuilder.DropTable(
                name: "users",
                schema: "core");

            migrationBuilder.DropTable(
                name: "waiting_list_items",
                schema: "core");

            migrationBuilder.DropTable(
                name: "families",
                schema: "core");

            migrationBuilder.DropTable(
                name: "retreats",
                schema: "core");

            migrationBuilder.DropTable(
                name: "teams",
                schema: "core");
        }
    }
}
