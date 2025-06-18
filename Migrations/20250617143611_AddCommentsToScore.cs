using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BluebirdCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentsToScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comments",
                table: "ExamScores",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CommentsUpdatedAt",
                table: "ExamScores",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommentsUpdatedBy",
                table: "ExamScores",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommentsUpdatedByTeacherId",
                table: "ExamScores",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamScores_CommentsUpdatedByTeacherId",
                table: "ExamScores",
                column: "CommentsUpdatedByTeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamScores_Users_CommentsUpdatedByTeacherId",
                table: "ExamScores",
                column: "CommentsUpdatedByTeacherId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamScores_Users_CommentsUpdatedByTeacherId",
                table: "ExamScores");

            migrationBuilder.DropIndex(
                name: "IX_ExamScores_CommentsUpdatedByTeacherId",
                table: "ExamScores");

            migrationBuilder.DropColumn(
                name: "Comments",
                table: "ExamScores");

            migrationBuilder.DropColumn(
                name: "CommentsUpdatedAt",
                table: "ExamScores");

            migrationBuilder.DropColumn(
                name: "CommentsUpdatedBy",
                table: "ExamScores");

            migrationBuilder.DropColumn(
                name: "CommentsUpdatedByTeacherId",
                table: "ExamScores");
        }
    }
}
