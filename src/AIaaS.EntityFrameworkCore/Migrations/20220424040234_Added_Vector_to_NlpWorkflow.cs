using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIaaS.Migrations
{
    public partial class Added_Vector_to_NlpWorkflow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Vector",
                table: "NlpWorkflows",
                type: "nvarchar(max)",
                maxLength: 65536,
                nullable: true);

            //migrationBuilder.AddForeignKey(
            //    name: "FK_NlpQAs_NlpWorkflows_CurrentWfState",
            //    table: "NlpQAs",
            //    column: "CurrentWfState",
            //    principalTable: "NlpWorkflows",
            //    principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_NlpQAs_NlpWorkflows_CurrentWfState",
            //    table: "NlpQAs");

            migrationBuilder.DropColumn(
                name: "Vector",
                table: "NlpWorkflows");
        }
    }
}
