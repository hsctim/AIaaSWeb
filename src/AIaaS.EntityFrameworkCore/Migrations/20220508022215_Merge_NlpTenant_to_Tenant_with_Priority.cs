using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIaaS.Migrations
{
    public partial class Merge_NlpTenant_to_Tenant_with_Priority : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NlpTenants");

            migrationBuilder.AddColumn<double>(
                name: "NlpPriority",
                table: "AbpTenants",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SubscriptionAmount",
                table: "AbpTenants",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NlpPriority",
                table: "AbpTenants");

            migrationBuilder.DropColumn(
                name: "SubscriptionAmount",
                table: "AbpTenants");

            migrationBuilder.CreateTable(
                name: "NlpTenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NlpPriority = table.Column<double>(type: "float", nullable: false),
                    SubscriptionAmount = table.Column<double>(type: "float", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NlpTenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NlpTenant_TenantId",
                table: "NlpTenants",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NlpTenants_TenantId",
                table: "NlpTenants",
                column: "TenantId");
        }
    }
}
