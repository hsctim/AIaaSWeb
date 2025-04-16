using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIaaS.Migrations
{
    public partial class delete_segments_from_nlpqa : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SegmentErrorMsg",
                table: "NlpQAs");

            migrationBuilder.DropColumn(
                name: "SegmentStatus",
                table: "NlpQAs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SegmentErrorMsg",
                table: "NlpQAs",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SegmentStatus",
                table: "NlpQAs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
