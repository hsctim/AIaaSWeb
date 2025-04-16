using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIaaS.Migrations
{
    public partial class Updated_NlpWorkflowStatus_FullAuditedEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreationTime",
                table: "NlpWorkflowStates",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "CreatorUserId",
                table: "NlpWorkflowStates",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeleterUserId",
                table: "NlpWorkflowStates",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionTime",
                table: "NlpWorkflowStates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "NlpWorkflowStates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModificationTime",
                table: "NlpWorkflowStates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LastModifierUserId",
                table: "NlpWorkflowStates",
                type: "bigint",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreationTime",
                table: "NlpWorkflowStates");

            migrationBuilder.DropColumn(
                name: "CreatorUserId",
                table: "NlpWorkflowStates");

            migrationBuilder.DropColumn(
                name: "DeleterUserId",
                table: "NlpWorkflowStates");

            migrationBuilder.DropColumn(
                name: "DeletionTime",
                table: "NlpWorkflowStates");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "NlpWorkflowStates");

            migrationBuilder.DropColumn(
                name: "LastModificationTime",
                table: "NlpWorkflowStates");

            migrationBuilder.DropColumn(
                name: "LastModifierUserId",
                table: "NlpWorkflowStates");
        }
    }
}
