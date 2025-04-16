using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIaaS.Migrations
{
    public partial class DeletedUnusedForeignKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            try
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_NlpCbQAAccuracies_NlpQAs_AnswerId1",
                    table: "NlpCbQAAccuracies");
                migrationBuilder.DropForeignKey(
                    name: "FK_NlpCbQAAccuracies_NlpQAs_AnswerId2",
                    table: "NlpCbQAAccuracies");
                migrationBuilder.DropForeignKey(
                    name: "FK_NlpCbQAAccuracies_NlpQAs_AnswerId3",
                    table: "NlpCbQAAccuracies");
                migrationBuilder.DropIndex(
                    name: "IX_NlpCbQAAccuracies_AnswerId1",
                    table: "NlpCbQAAccuracies");
                migrationBuilder.DropIndex(
                    name: "IX_NlpCbQAAccuracies_AnswerId2",
                    table: "NlpCbQAAccuracies");
                migrationBuilder.DropIndex(
                    name: "IX_NlpCbQAAccuracies_AnswerId3",
                    table: "NlpCbQAAccuracies");

                migrationBuilder.DropForeignKey(
                    name: "FK_NlpQAs_NlpWorkflowStates_CurrentWfState",
                    table: "NlpQAs");
                migrationBuilder.DropForeignKey(
                    name: "FK_NlpQAs_NlpWorkflowStates_NextWfState",
                    table: "NlpQAs");
            }
            catch (System.Exception)
            {
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
