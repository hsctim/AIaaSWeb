using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIaaS.Migrations
{
    public partial class Renamed_WorkflowStatus_to_WorkflowState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            return;
            migrationBuilder.DropForeignKey(
                name: "FK_NlpQAs_NlpWorkflowStates_CurrentWfState",
                table: "NlpQAs");

            migrationBuilder.DropForeignKey(
                name: "FK_NlpQAs_NlpWorkflowStates_NextWfState",
                table: "NlpQAs");

            migrationBuilder.DropTable(
                name: "NlpWorkflowStates");

            migrationBuilder.RenameColumn(
                name: "NextWfState",
                table: "NlpQAs",
                newName: "NextWfState");

            migrationBuilder.RenameColumn(
                name: "CurrentWfState",
                table: "NlpQAs",
                newName: "CurrentWfState");

            migrationBuilder.RenameIndex(
                name: "IX_NlpQAs_NextWfState",
                table: "NlpQAs",
                newName: "IX_NlpQAs_NextWfState");

            migrationBuilder.RenameIndex(
                name: "IX_NlpQAs_CurrentWfState",
                table: "NlpQAs",
                newName: "IX_NlpQAs_CurrentWfState");

            migrationBuilder.CreateTable(
                name: "NlpWorkflowStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    StateName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    StateInstruction = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Vector = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: true),
                    OutgoingFalseOp = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: true),
                    Outgoing3FalseOp = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: true),
                    ResponseNonWorkflowAnswer = table.Column<bool>(type: "bit", nullable: false),
                    DontResponseNonWorkflowErrorAnswer = table.Column<bool>(type: "bit", nullable: false),
                    NlpWorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NlpWorkflowStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NlpWorkflowStates_NlpWorkflows_NlpWorkflowId",
                        column: x => x.NlpWorkflowId,
                        principalTable: "NlpWorkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NlpWorkflowStates_NlpWorkflowId",
                table: "NlpWorkflowStates",
                column: "NlpWorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpWorkflowStates_TenantId",
                table: "NlpWorkflowStates",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_NlpQAs_NlpWorkflowStates_CurrentWfState",
                table: "NlpQAs",
                column: "CurrentWfState",
                principalTable: "NlpWorkflowStates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NlpQAs_NlpWorkflowStates_NextWfState",
                table: "NlpQAs",
                column: "NextWfState",
                principalTable: "NlpWorkflowStates",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            return;
            migrationBuilder.DropForeignKey(
                name: "FK_NlpQAs_NlpWorkflowStates_CurrentWfState",
                table: "NlpQAs");

            migrationBuilder.DropForeignKey(
                name: "FK_NlpQAs_NlpWorkflowStates_NextWfState",
                table: "NlpQAs");

            migrationBuilder.DropTable(
                name: "NlpWorkflowStates");

            migrationBuilder.RenameColumn(
                name: "NextWfState",
                table: "NlpQAs",
                newName: "NextWfState");

            migrationBuilder.RenameColumn(
                name: "CurrentWfState",
                table: "NlpQAs",
                newName: "CurrentWfState");

            migrationBuilder.RenameIndex(
                name: "IX_NlpQAs_NextWfState",
                table: "NlpQAs",
                newName: "IX_NlpQAs_NextWfState");

            migrationBuilder.RenameIndex(
                name: "IX_NlpQAs_CurrentWfState",
                table: "NlpQAs",
                newName: "IX_NlpQAs_CurrentWfState");

            migrationBuilder.CreateTable(
                name: "NlpWorkflowStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NlpWorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DontResponseNonWorkflowErrorAnswer = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    Outgoing3FalseOp = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: true),
                    OutgoingFalseOp = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: true),
                    ResponseNonWorkflowAnswer = table.Column<bool>(type: "bit", nullable: false),
                    StateInstruction = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    StateName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Vector = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NlpWorkflowStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NlpWorkflowStates_NlpWorkflows_NlpWorkflowId",
                        column: x => x.NlpWorkflowId,
                        principalTable: "NlpWorkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NlpWorkflowStates_NlpWorkflowId",
                table: "NlpWorkflowStates",
                column: "NlpWorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpWorkflowStates_TenantId",
                table: "NlpWorkflowStates",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_NlpQAs_NlpWorkflowStates_CurrentWfState",
                table: "NlpQAs",
                column: "CurrentWfState",
                principalTable: "NlpWorkflowStates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NlpQAs_NlpWorkflowStates_NextWfState",
                table: "NlpQAs",
                column: "NextWfState",
                principalTable: "NlpWorkflowStates",
                principalColumn: "Id");
        }
    }
}
