using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BluebirdCore.Migrations
{
    /// <inheritdoc />
    public partial class PdfContentPropertyToReportCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "ReportCards");

            migrationBuilder.AddColumn<byte[]>(
                name: "PdfContent",
                table: "ReportCards",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdfContent",
                table: "ReportCards");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "ReportCards",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
