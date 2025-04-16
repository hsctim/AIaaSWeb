using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIaaS.Migrations
{
    public partial class addedopenai : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EnableHttpChat",
                table: "NlpChatbots",
                newName: "EnableWebAPI");

            migrationBuilder.AddColumn<int>(
                name: "EnableOPENAI",
                table: "NlpChatbots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "OPENAICache", 
                table: "NlpChatbots",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableWebhook",
                table: "NlpChatbots",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OPENAIOrg",
                table: "NlpChatbots",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OPENAISeret",
                table: "NlpChatbots",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebApiSecret",
                table: "NlpChatbots",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebhookSecret",
                table: "NlpChatbots",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableOPENAI",
                table: "NlpChatbots");

            migrationBuilder.DropColumn(
                name: "OPENAICache",
                table: "NlpChatbots");

            migrationBuilder.DropColumn(
                name: "EnableWebhook",
                table: "NlpChatbots");

            migrationBuilder.DropColumn(
                name: "OPENAIOrg",
                table: "NlpChatbots");

            migrationBuilder.DropColumn(
                name: "OPENAISeret",
                table: "NlpChatbots");

            migrationBuilder.DropColumn(
                name: "WebApiSecret",
                table: "NlpChatbots");

            migrationBuilder.DropColumn(
                name: "WebhookSecret",
                table: "NlpChatbots");

            migrationBuilder.RenameColumn(
                name: "EnableWebAPI",
                table: "NlpChatbots",
                newName: "EnableHttpChat");
        }
    }
}
