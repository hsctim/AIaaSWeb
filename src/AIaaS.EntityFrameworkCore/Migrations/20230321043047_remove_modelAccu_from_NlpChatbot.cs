using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIaaS.Migrations
{
    public partial class remove_modelAccu_from_NlpChatbot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModelAccu",
                table: "NlpChatbots");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "ModelAccu",
                table: "NlpChatbots",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }
    }
}
