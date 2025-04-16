using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIaaS.Migrations
{
    public partial class Added_Accu_Threshold_to_NlpChatbot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "ModelAccu",
                table: "NlpChatbots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "PredThreshold",
                table: "NlpChatbots",
                type: "real",
                nullable: false,
                defaultValue: 0.5f);

            migrationBuilder.AddColumn<float>(
                name: "SuggestionThreshold",
                table: "NlpChatbots",
                type: "real",
                nullable: false,
                defaultValue: 0.5f);

            migrationBuilder.AddColumn<float>(
                name: "WSPredThreshold",
                table: "NlpChatbots",
                type: "real",
                nullable: false,
                defaultValue: 0.7f);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModelAccu",
                table: "NlpChatbots");

            migrationBuilder.DropColumn(
                name: "PredThreshold",
                table: "NlpChatbots");

            migrationBuilder.DropColumn(
                name: "SuggestionThreshold",
                table: "NlpChatbots");

            migrationBuilder.DropColumn(
                name: "WSPredThreshold",
                table: "NlpChatbots");
        }
    }
}
