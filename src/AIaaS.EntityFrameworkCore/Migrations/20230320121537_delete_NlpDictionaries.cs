using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIaaS.Migrations
{
    public partial class delete_NlpDictionaries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NlpCbDictionaries");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NlpCbDictionaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NlpChatbotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HostDicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    Synonym = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Vector = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: true),
                    Word = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NlpCbDictionaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NlpCbDictionaries_NlpChatbots_NlpChatbotId",
                        column: x => x.NlpChatbotId,
                        principalTable: "NlpChatbots",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbDictionaries_NlpChatbotId",
                table: "NlpCbDictionaries",
                column: "NlpChatbotId");

            migrationBuilder.CreateIndex(
                name: "IX_NlpCbDictionaries_TenantId",
                table: "NlpCbDictionaries",
                column: "TenantId");
        }
    }
}
