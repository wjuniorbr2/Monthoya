using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monthoya.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Body = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduledForUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TriggeredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RequiresAcknowledgement = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystemGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    RelatedEntityType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActionLabel = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ActionTarget = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_messages_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "notification_deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    Destination = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_deliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_deliveries_notification_messages_NotificationM~",
                        column: x => x.NotificationMessageId,
                        principalTable: "notification_messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notification_deliveries_users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "notification_recipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DismissedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_recipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_recipients_notification_messages_NotificationM~",
                        column: x => x.NotificationMessageId,
                        principalTable: "notification_messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notification_recipients_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_NotificationMessageId_Channel",
                table: "notification_deliveries",
                columns: new[] { "NotificationMessageId", "Channel" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_RecipientUserId_Status",
                table: "notification_deliveries",
                columns: new[] { "RecipientUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_messages_Category_RelatedEntityType_RelatedEnt~",
                table: "notification_messages",
                columns: new[] { "Category", "RelatedEntityType", "RelatedEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_messages_CreatedAtUtc",
                table: "notification_messages",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_notification_messages_CreatedByUserId",
                table: "notification_messages",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_messages_ScheduledForUtc",
                table: "notification_messages",
                column: "ScheduledForUtc");

            migrationBuilder.CreateIndex(
                name: "IX_notification_recipients_NotificationMessageId_UserId",
                table: "notification_recipients",
                columns: new[] { "NotificationMessageId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_recipients_UserId_AcknowledgedAtUtc",
                table: "notification_recipients",
                columns: new[] { "UserId", "AcknowledgedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_recipients_UserId_ReadAtUtc",
                table: "notification_recipients",
                columns: new[] { "UserId", "ReadAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_deliveries");

            migrationBuilder.DropTable(
                name: "notification_recipients");

            migrationBuilder.DropTable(
                name: "notification_messages");
        }
    }
}
