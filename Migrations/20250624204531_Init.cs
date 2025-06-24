using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BluebirdCore.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicYears",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicYears", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExamTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Grades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Stream = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Section = table.Column<int>(type: "int", nullable: false),
                    HomeroomTeacherId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CurriculumType = table.Column<int>(type: "int", nullable: false),
                    IsTransitional = table.Column<bool>(type: "bit", nullable: false),
                    PhaseOutYear = table.Column<int>(type: "int", nullable: true),
                    IntroducedYear = table.Column<int>(type: "int", nullable: true),
                    ValidForCohorts = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grades_Users_HomeroomTeacherId",
                        column: x => x.HomeroomTeacherId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GradeSubjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GradeId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    IsOptional = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeSubjects_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GradeSubjects_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StudentNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    GuardianName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GuardianPhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    GradeId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    EnrollmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchiveDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Students_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeacherSubjectAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    GradeId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherSubjectAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherSubjectAssignments_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherSubjectAssignments_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherSubjectAssignments_Users_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    ExamTypeId = table.Column<int>(type: "int", nullable: false),
                    GradeId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AcademicYear = table.Column<int>(type: "int", nullable: false),
                    Term = table.Column<int>(type: "int", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedBy = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CommentsUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CommentsUpdatedBy = table.Column<int>(type: "int", nullable: true),
                    CommentsUpdatedByTeacherId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamScores_ExamTypes_ExamTypeId",
                        column: x => x.ExamTypeId,
                        principalTable: "ExamTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamScores_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamScores_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamScores_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamScores_Users_CommentsUpdatedByTeacherId",
                        column: x => x.CommentsUpdatedByTeacherId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamScores_Users_RecordedBy",
                        column: x => x.RecordedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    GradeId = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<int>(type: "int", nullable: false),
                    Term = table.Column<int>(type: "int", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportCards_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportCards_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportCards_Users_GeneratedBy",
                        column: x => x.GeneratedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentOptionalSubjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentOptionalSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentOptionalSubjects_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentOptionalSubjects_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AcademicYears",
                columns: new[] { "Id", "EndDate", "IsActive", "IsClosed", "Name", "StartDate" },
                values: new object[] { 1, new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, false, "2025", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.InsertData(
                table: "ExamTypes",
                columns: new[] { "Id", "Description", "IsActive", "Name", "Order" },
                values: new object[,]
                {
                    { 1, "First test of the term", true, "Test-One", 1 },
                    { 2, "Second-test examination", true, "Test-Two", 2 },
                    { 3, "End of term examination", true, "End-of-Term", 3 }
                });

            migrationBuilder.InsertData(
                table: "Grades",
                columns: new[] { "Id", "CurriculumType", "HomeroomTeacherId", "IntroducedYear", "IsActive", "IsTransitional", "Level", "Name", "PhaseOutYear", "Section", "Stream", "ValidForCohorts" },
                values: new object[,]
                {
                    { 1, 0, null, null, true, false, 0, "Baby-Class", null, 0, "Purple", null },
                    { 2, 0, null, null, true, false, 1, "Baby-Class", null, 0, "Green", null },
                    { 3, 0, null, null, true, false, 2, "Baby-Class", null, 0, "Orange", null },
                    { 4, 0, null, null, true, false, 3, "Middle-Class", null, 0, "Purple", null },
                    { 5, 0, null, null, true, false, 4, "Middle-Class", null, 0, "Green", null },
                    { 6, 0, null, null, true, false, 5, "Middle-Class", null, 0, "Orange", null },
                    { 7, 0, null, null, true, false, 6, "Reception-Class", null, 0, "Purple", null },
                    { 8, 0, null, null, true, false, 7, "Reception-Class", null, 0, "Green", null },
                    { 9, 0, null, null, true, false, 8, "Reception-Class", null, 0, "Orange", null },
                    { 10, 0, null, null, true, false, 9, "Grade 1", null, 1, "Purple", null },
                    { 11, 0, null, null, true, false, 10, "Grade 1", null, 1, "Green", null },
                    { 12, 0, null, null, true, false, 11, "Grade 1", null, 1, "Orange", null },
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

            migrationBuilder.InsertData(
                table: "Subjects",
                columns: new[] { "Id", "Code", "Description", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "MATH", null, true, "Mathematics" },
                    { 2, "ENG", null, true, "English" },
                    { 3, "SCI", null, true, "Science" },
                    { 4, "SS", null, true, "Social Studies" },
                    { 5, "FR", null, true, "French" },
                    { 6, "ICT", null, true, "ICT" },
                    { 7, "PE", null, true, "Physical Education" },
                    { 8, "ART", null, true, "Art" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "LastLoginAt", "PasswordHash", "Role", "Username" },
                values: new object[] { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@school.edu", "System Administrator", true, null, "$2a$12$Y5Cr10SW4OuJq6qxj7PXtOhZvb7loVQqIRRwcrH8hsdsoeRCririq", 1, "admin" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamScores_CommentsUpdatedByTeacherId",
                table: "ExamScores",
                column: "CommentsUpdatedByTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamScores_ExamTypeId",
                table: "ExamScores",
                column: "ExamTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamScores_GradeId",
                table: "ExamScores",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamScores_RecordedBy",
                table: "ExamScores",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExamScores_StudentId_SubjectId_ExamTypeId_AcademicYear_Term",
                table: "ExamScores",
                columns: new[] { "StudentId", "SubjectId", "ExamTypeId", "AcademicYear", "Term" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamScores_SubjectId",
                table: "ExamScores",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_HomeroomTeacherId",
                table: "Grades",
                column: "HomeroomTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeSubjects_GradeId_SubjectId",
                table: "GradeSubjects",
                columns: new[] { "GradeId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeSubjects_SubjectId",
                table: "GradeSubjects",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportCards_GeneratedBy",
                table: "ReportCards",
                column: "GeneratedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReportCards_GradeId",
                table: "ReportCards",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportCards_StudentId",
                table: "ReportCards",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentOptionalSubjects_StudentId_SubjectId",
                table: "StudentOptionalSubjects",
                columns: new[] { "StudentId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentOptionalSubjects_SubjectId",
                table: "StudentOptionalSubjects",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_GradeId",
                table: "Students",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentNumber",
                table: "Students",
                column: "StudentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectAssignments_GradeId",
                table: "TeacherSubjectAssignments",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectAssignments_SubjectId",
                table: "TeacherSubjectAssignments",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectAssignments_TeacherId",
                table: "TeacherSubjectAssignments",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicYears");

            migrationBuilder.DropTable(
                name: "ExamScores");

            migrationBuilder.DropTable(
                name: "GradeSubjects");

            migrationBuilder.DropTable(
                name: "ReportCards");

            migrationBuilder.DropTable(
                name: "StudentOptionalSubjects");

            migrationBuilder.DropTable(
                name: "TeacherSubjectAssignments");

            migrationBuilder.DropTable(
                name: "ExamTypes");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "Grades");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
