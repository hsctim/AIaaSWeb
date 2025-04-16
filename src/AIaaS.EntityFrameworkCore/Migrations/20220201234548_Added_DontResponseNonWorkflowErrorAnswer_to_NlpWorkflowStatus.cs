using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIaaS.Migrations
{
    public partial class Added_DontResponseNonWorkflowErrorAnswer_to_NlpWorkflowStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DontResponseNonWorkflowErrorAnswer",
                table: "NlpWorkflowStates",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DontResponseNonWorkflowErrorAnswer",
                table: "NlpWorkflowStates");
        }
    }
}
