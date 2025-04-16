using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIaaS.Migrations
{
    public partial class Added_WorkflowStatus_To_NlpMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "QAId",
                table: "NlpCbMessages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbMessages_QAId",
                table: "NlpCbMessages",
                column: "QAId");

            migrationBuilder.AddForeignKey(
                name: "FK_NlpCbMessages_NlpQAs_QAId",
                table: "NlpCbMessages",
                column: "QAId",
                principalTable: "NlpQAs",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NlpCbMessages_NlpQAs_QAId",
                table: "NlpCbMessages");

            migrationBuilder.DropIndex(
                name: "IX_NlpCbMessages_QAId",
                table: "NlpCbMessages");

            migrationBuilder.DropColumn(
                name: "QAId",
                table: "NlpCbMessages");
        }
    }
}
