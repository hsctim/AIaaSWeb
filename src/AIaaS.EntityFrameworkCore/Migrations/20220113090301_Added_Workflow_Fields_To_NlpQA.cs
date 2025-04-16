using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIaaS.Migrations
{
    public partial class Added_Workflow_Fields_To_NlpQA : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentWfState",
                table: "NlpQAs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NextWfState",
                table: "NlpQAs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NlpQAs_CurrentWfState",
                table: "NlpQAs",
                column: "CurrentWfState");

            migrationBuilder.CreateIndex(
                name: "IX_NlpQAs_NextWfState",
                table: "NlpQAs",
                column: "NextWfState");

            migrationBuilder.AddForeignKey(
                name: "FK_NlpQAs_NlpWorkflowStates_CurrentWfState",
                table: "NlpQAs",
                column: "CurrentWfState",
                principalTable: "NlpWorkflowStates",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_NlpQAs_NlpWorkflowStates_NextWfState",
                table: "NlpQAs",
                column: "NextWfState",
                principalTable: "NlpWorkflowStates",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NlpQAs_NlpWorkflowStates_CurrentWfState",
                table: "NlpQAs");

            migrationBuilder.DropForeignKey(
                name: "FK_NlpQAs_NlpWorkflowStates_NextWfState",
                table: "NlpQAs");

            migrationBuilder.DropIndex(
                name: "IX_NlpQAs_CurrentWfState",
                table: "NlpQAs");

            migrationBuilder.DropIndex(
                name: "IX_NlpQAs_NextWfState",
                table: "NlpQAs");

            migrationBuilder.DropColumn(
                name: "CurrentWfState",
                table: "NlpQAs");

            migrationBuilder.DropColumn(
                name: "NextWfState",
                table: "NlpQAs");
        }
    }
}
