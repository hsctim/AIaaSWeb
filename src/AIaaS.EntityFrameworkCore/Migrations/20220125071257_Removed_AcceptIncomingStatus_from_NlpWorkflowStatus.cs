using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIaaS.Migrations
{
    public partial class Removed_AcceptIncomingStatus_from_NlpWorkflowStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptIncomingStatus",
                table: "NlpWorkflowStates");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcceptIncomingStatus",
                table: "NlpWorkflowStates",
                type: "nvarchar(max)",
                maxLength: 65536,
                nullable: true);
        }
    }
}
