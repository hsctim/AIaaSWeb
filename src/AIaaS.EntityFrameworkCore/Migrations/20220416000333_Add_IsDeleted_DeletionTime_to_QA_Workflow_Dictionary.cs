using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIaaS.Migrations
{
    public partial class Add_IsDeleted_DeletionTime_to_QA_Workflow_Dictionary : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeleterUserId",
                table: "NlpWorkflowStates");

            migrationBuilder.DropColumn(
                name: "DeleterUserId",
                table: "NlpWorkflows");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionTime",
                table: "NlpQAs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionTime",
                table: "NlpCbDictionaries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "NlpCbDictionaries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModificationTime",
                table: "NlpCbDictionaries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LastModifierUserId",
                table: "NlpCbDictionaries",
                type: "bigint",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletionTime",
                table: "NlpQAs");

            migrationBuilder.DropColumn(
                name: "DeletionTime",
                table: "NlpCbDictionaries");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "NlpCbDictionaries");

            migrationBuilder.DropColumn(
                name: "LastModificationTime",
                table: "NlpCbDictionaries");

            migrationBuilder.DropColumn(
                name: "LastModifierUserId",
                table: "NlpCbDictionaries");

            migrationBuilder.AddColumn<long>(
                name: "DeleterUserId",
                table: "NlpWorkflowStates",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeleterUserId",
                table: "NlpWorkflows",
                type: "bigint",
                nullable: true);
        }
    }
}
