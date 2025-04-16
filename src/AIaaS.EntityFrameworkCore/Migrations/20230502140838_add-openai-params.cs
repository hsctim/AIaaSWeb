using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIaaS.Migrations
{
    public partial class addopenaiparams : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OPENAISeret",
                table: "NlpChatbots",
                newName: "OPENAIKey");

            migrationBuilder.AddColumn<string>(
                name: "OpenAIParam",
                table: "NlpChatbots",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpenAIParam",
                table: "NlpChatbots");

            migrationBuilder.RenameColumn(
                name: "OPENAIKey",
                table: "NlpChatbots",
                newName: "OPENAISeret");
        }
    }
}
