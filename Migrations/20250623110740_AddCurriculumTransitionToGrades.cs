using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BluebirdCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCurriculumTransitionToGrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurriculumType",
                table: "Grades",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IntroducedYear",
                table: "Grades",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTransitional",
                table: "Grades",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PhaseOutYear",
                table: "Grades",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidForCohorts",
                table: "Grades",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AcademicYears",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EndDate", "Name", "StartDate" },
                values: new object[] { new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "2025", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "ExamTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Second-test examination", "Test-Two" });

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CurriculumType", "IntroducedYear", "IsTransitional", "Level", "PhaseOutYear", "ValidForCohorts" },
                values: new object[] { 0, null, false, 0, null, null });

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CurriculumType", "IntroducedYear", "IsTransitional", "Level", "PhaseOutYear", "ValidForCohorts" },
                values: new object[] { 0, null, false, 1, null, null });

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CurriculumType", "IntroducedYear", "IsTransitional", "Level", "PhaseOutYear", "ValidForCohorts" },
                values: new object[] { 0, null, false, 2, null, null });

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CurriculumType", "IntroducedYear", "IsTransitional", "Level", "PhaseOutYear", "ValidForCohorts" },
                values: new object[] { 0, null, false, 3, null, null });

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CurriculumType", "IntroducedYear", "IsTransitional", "Level", "PhaseOutYear", "ValidForCohorts" },
                values: new object[] { 0, null, false, 4, null, null });

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CurriculumType", "IntroducedYear", "IsTransitional", "Level", "PhaseOutYear", "ValidForCohorts" },
                values: new object[] { 0, null, false, 5, null, null });

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CurriculumType", "IntroducedYear", "IsTransitional", "Level", "PhaseOutYear", "ValidForCohorts" },
                values: new object[] { 0, null, false, 6, null, null });

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CurriculumType", "IntroducedYear", "IsTransitional", "Level", "PhaseOutYear", "ValidForCohorts" },
                values: new object[] { 0, null, false, 7, null, null });

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CurriculumType", "IntroducedYear", "IsTransitional", "Level", "PhaseOutYear", "ValidForCohorts" },
                values: new object[] { 0, null, false, 8, null, null });

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CurriculumType", "IntroducedYear", "IsTransitional", "Level", "PhaseOutYear", "ValidForCohorts" },
                values: new object[] { 0, null, false, 9, null, null });

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CurriculumType", "IntroducedYear", "IsTransitional", "Level", "PhaseOutYear", "ValidForCohorts" },
                values: new object[] { 0, null, false, 10, null, null });

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CurriculumType", "IntroducedYear", "IsTransitional", "Level", "PhaseOutYear", "ValidForCohorts" },
                values: new object[] { 0, null, false, 11, null, null });

            migrationBuilder.InsertData(
                table: "Grades",
                columns: new[] { "Id", "CurriculumType", "HomeroomTeacherId", "IntroducedYear", "IsActive", "IsTransitional", "Level", "Name", "PhaseOutYear", "Section", "Stream", "ValidForCohorts" },
                values: new object[,]
                {
                    { 13, 0, null, null, true, false, 12, "Grade 2", null, 1, "Purple", null },
                    { 14, 0, null, null, true, false, 13, "Grade 2", null, 1, "Green", null },
                    { 15, 0, null, null, true, false, 14, "Grade 2", null, 1, "Orange", null },
                    { 16, 0, null, null, true, false, 15, "Grade 3", null, 1, "Purple", null },
                    { 17, 0, null, null, true, false, 16, "Grade 3", null, 1, "Green", null },
                    { 18, 0, null, null, true, false, 17, "Grade 3", null, 1, "Orange", null },
                    { 19, 0, null, null, true, false, 18, "Grade 4", null, 1, "Purple", null },
                    { 20, 0, null, null, true, false, 19, "Grade 4", null, 1, "Green", null },
                    { 21, 0, null, null, true, false, 20, "Grade 4", null, 1, "Orange", null },
                    { 22, 0, null, null, true, false, 21, "Grade 5", null, 1, "Purple", null },
                    { 23, 0, null, null, true, false, 22, "Grade 5", null, 1, "Green", null },
                    { 24, 0, null, null, true, false, 23, "Grade 5", null, 1, "Orange", null },
                    { 25, 0, null, null, true, false, 24, "Grade 6", null, 1, "Purple", null },
                    { 26, 0, null, null, true, false, 25, "Grade 6", null, 1, "Green", null },
                    { 27, 0, null, null, true, false, 26, "Grade 6", null, 1, "Orange", null },
                    { 28, 0, null, null, true, true, 27, "Grade 7", 2028, 1, "Purple", "2025,2026,2027,2028" },
                    { 29, 0, null, null, true, true, 28, "Grade 7", 2028, 1, "Green", "2025,2026,2027,2028" },
                    { 30, 0, null, null, true, true, 29, "Grade 7", 2028, 1, "Orange", "2025,2026,2027,2028" },
                    { 31, 0, null, null, true, false, 30, "Grade 8", null, 2, "Grey", null },
                    { 32, 0, null, null, true, false, 31, "Grade 8", null, 2, "Blue", null },
                    { 33, 0, null, null, true, false, 32, "Grade 9", null, 2, "Grey", null },
                    { 34, 0, null, null, true, false, 33, "Grade 9", null, 2, "Blue", null },
                    { 35, 0, null, null, true, false, 34, "Grade 10", null, 2, "Grey", null },
                    { 36, 0, null, null, true, false, 35, "Grade 10", null, 2, "Blue", null },
                    { 37, 0, null, null, true, false, 36, "Grade 11", null, 2, "Grey", null },
                    { 38, 0, null, null, true, false, 37, "Grade 11", null, 2, "Blue", null },
                    { 39, 0, null, null, true, false, 38, "Grade 12", null, 2, "Grey", null },
                    { 40, 0, null, null, true, false, 39, "Grade 12", null, 2, "Blue", null },
                    { 41, 1, null, 2025, true, false, 27, "Form 1", null, 2, "Grey", null },
                    { 42, 1, null, 2025, true, false, 28, "Form 1", null, 2, "Blue", null },
                    { 43, 1, null, 2026, true, false, 30, "Form 2", null, 2, "Grey", null },
                    { 44, 1, null, 2026, true, false, 31, "Form 2", null, 2, "Blue", null },
                    { 45, 1, null, 2027, true, false, 33, "Form 3", null, 2, "Grey", null },
                    { 46, 1, null, 2027, true, false, 34, "Form 3", null, 2, "Blue", null },
                    { 47, 1, null, 2028, true, false, 36, "Form 4", null, 2, "Grey", null },
                    { 48, 1, null, 2028, true, false, 37, "Form 4", null, 2, "Blue", null },
                    { 49, 1, null, 2029, true, false, 39, "Form 5", null, 2, "Grey", null },
                    { 50, 1, null, 2029, true, false, 40, "Form 5", null, 2, "Blue", null },
                    { 51, 1, null, 2030, true, false, 39, "Form 6", null, 2, "Grey", null },
                    { 52, 1, null, 2030, true, false, 40, "Form 6", null, 2, "Blue", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DropColumn(
                name: "CurriculumType",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "IntroducedYear",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "IsTransitional",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "PhaseOutYear",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "ValidForCohorts",
                table: "Grades");

            migrationBuilder.UpdateData(
                table: "AcademicYears",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EndDate", "Name", "StartDate" },
                values: new object[] { new DateTime(2024, 12, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "2024-2025", new DateTime(2024, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "ExamTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Mid-term examination", "Mid-Term" });

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 1,
                column: "Level",
                value: -3);

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 2,
                column: "Level",
                value: -2);

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 3,
                column: "Level",
                value: -1);

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 4,
                column: "Level",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 5,
                column: "Level",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 6,
                column: "Level",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 7,
                column: "Level",
                value: 3);

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 8,
                column: "Level",
                value: 4);

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 9,
                column: "Level",
                value: 5);

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 10,
                column: "Level",
                value: 6);

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 11,
                column: "Level",
                value: 7);

            migrationBuilder.UpdateData(
                table: "Grades",
                keyColumn: "Id",
                keyValue: 12,
                column: "Level",
                value: 8);
        }
    }
}
