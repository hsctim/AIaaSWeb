using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIaaS.Migrations
{
    public partial class Add_Unique_NlpTenant_TenantId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_NlpTenant_TenantId",
                table: "NlpTenants",
                column: "TenantId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NlpTenant_TenantId",
                table: "NlpTenants");
        }
    }
}
