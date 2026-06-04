using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monthoya.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationEmailSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_email_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SenderDisplayName = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: true),
                    SenderEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    SmtpHost = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: true),
                    SmtpPort = table.Column<int>(type: "integer", nullable: false),
                    UseSslTls = table.Column<bool>(type: "boolean", nullable: false),
                    SmtpUsername = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    SmtpPasswordSecret = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReplyToEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_email_settings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_email_settings");
        }
    }
}
