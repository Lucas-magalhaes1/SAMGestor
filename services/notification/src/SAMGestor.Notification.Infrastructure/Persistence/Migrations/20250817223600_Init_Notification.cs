using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Notification.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init_Notification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notification");

            migrationBuilder.CreateTable(
                name: "notification_messages",
                schema: "notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RecipientEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    RecipientPhone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    TemplateKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Body = table.Column<string>(type: "text", nullable: true),
                    RegistrationId = table.Column<Guid>(type: "uuid", nullable: true),
                    RetreatId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalCorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notification_dispatch_logs",
                schema: "notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_dispatch_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_dispatch_logs_notification_messages_Notificati~",
                        column: x => x.NotificationMessageId,
                        principalSchema: "notification",
                        principalTable: "notification_messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dispatch_message",
                schema: "notification",
                table: "notification_dispatch_logs",
                column: "NotificationMessageId");

            migrationBuilder.CreateIndex(
                name: "ix_notification_extcorr",
                schema: "notification",
                table: "notification_messages",
                column: "ExternalCorrelationId");

            migrationBuilder.CreateIndex(
                name: "ix_notification_registration",
                schema: "notification",
                table: "notification_messages",
                column: "RegistrationId");

            migrationBuilder.CreateIndex(
                name: "ix_notification_retreat",
                schema: "notification",
                table: "notification_messages",
                column: "RetreatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_dispatch_logs",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "notification_messages",
                schema: "notification");
        }
    }
}
