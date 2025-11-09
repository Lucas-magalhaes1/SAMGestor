using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Reports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reports",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    template_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    retreat_id = table.Column<Guid>(type: "uuid", nullable: true),
                    date_creation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_update = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_params_json = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reports_retreats_retreat_id",
                        column: x => x.retreat_id,
                        principalSchema: "core",
                        principalTable: "retreats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "report_instances",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    file_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    storage_path = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_instances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_instances_reports_report_id",
                        column: x => x.report_id,
                        principalSchema: "core",
                        principalTable: "reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_report_instances_report_date",
                schema: "core",
                table: "report_instances",
                columns: new[] { "report_id", "generated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reports_date_creation",
                schema: "core",
                table: "reports",
                column: "date_creation");

            migrationBuilder.CreateIndex(
                name: "ix_reports_retreat_template",
                schema: "core",
                table: "reports",
                columns: new[] { "retreat_id", "template_key" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_instances",
                schema: "core");

            migrationBuilder.DropTable(
                name: "reports",
                schema: "core");
        }
    }
}
