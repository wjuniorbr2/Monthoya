using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monthoya.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLoginName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LoginName",
                table: "users",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedLoginName",
                table: "users",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE users
                SET "LoginName" = LEFT(SPLIT_PART("Email", '@', 1), 80),
                    "NormalizedLoginName" = LEFT(UPPER(SPLIT_PART("Email", '@', 1)), 80)
                WHERE "LoginName" = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_users_NormalizedLoginName",
                table: "users",
                column: "NormalizedLoginName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_NormalizedLoginName",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LoginName",
                table: "users");

            migrationBuilder.DropColumn(
                name: "NormalizedLoginName",
                table: "users");
        }
    }
}
