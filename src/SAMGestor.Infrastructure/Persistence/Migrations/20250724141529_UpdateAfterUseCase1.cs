using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAfterUseCase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "total_slots",
                table: "retreats",
                newName: "male_slots");

            migrationBuilder.AddColumn<bool>(
                name: "contemplation_closed",
                table: "retreats",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "fee_fazer_amount",
                table: "retreats",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "fee_fazer_currency",
                table: "retreats",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "fee_servir_amount",
                table: "retreats",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "fee_servir_currency",
                table: "retreats",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "female_slots",
                table: "retreats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "blocked_cpfs",
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
                name: "region_configs",
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
                name: "waiting_list_items",
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

            migrationBuilder.CreateIndex(
                name: "IX_blocked_cpfs_cpf",
                table: "blocked_cpfs",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_region_configs_retreat_id_name",
                table: "region_configs",
                columns: new[] { "retreat_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_waiting_list_items_registration_id",
                table: "waiting_list_items",
                column: "registration_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_waiting_list_items_retreat_id_position",
                table: "waiting_list_items",
                columns: new[] { "retreat_id", "position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blocked_cpfs");

            migrationBuilder.DropTable(
                name: "region_configs");

            migrationBuilder.DropTable(
                name: "waiting_list_items");

            migrationBuilder.DropColumn(
                name: "contemplation_closed",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "fee_fazer_amount",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "fee_fazer_currency",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "fee_servir_amount",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "fee_servir_currency",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "female_slots",
                table: "retreats");

            migrationBuilder.RenameColumn(
                name: "male_slots",
                table: "retreats",
                newName: "total_slots");
        }
    }
}
