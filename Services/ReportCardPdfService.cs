// ===== CORRECTED QUESTPDF REPORT CARD SERVICE =====

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using BluebirdCore.Entities;
using Microsoft.EntityFrameworkCore;
using BluebirdCore.Data;

namespace BluebirdCore.Services
{
    public class ReportCardPdfService
    {
        private readonly string _reportPath;
        private readonly SchoolDbContext _context;
        private readonly ILogger<ReportCardPdfService> _logger;

        public ReportCardPdfService(IConfiguration configuration, SchoolDbContext context, ILogger<ReportCardPdfService> logger)
        {
            _reportPath = configuration["ReportCards:StoragePath"] ?? "Reports";
            _context = context;
            _logger = logger;
            Directory.CreateDirectory(_reportPath);

            // Configure QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<string> GenerateReportCardPdfAsync(Student student, IEnumerable<ExamScore> scores, int academicYear, int term)
        {
            var fileName = $"ReportCard_{student.StudentNumber}_{academicYear}_Term{term}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            var filePath = Path.Combine(_reportPath, fileName);

            try
            {
                // Get additional data needed for report
                var grade = await _context.Grades
                    .Include(g => g.HomeroomTeacher)
                    .FirstOrDefaultAsync(g => g.Id == student.GradeId);

                var academicYearInfo = await _context.AcademicYears
                    .FirstOrDefaultAsync(ay => ay.Name.Contains(academicYear.ToString()));

                // Generate PDF based on school section
                switch (student.Grade.Section)
                {
                    case SchoolSection.Preschool:
                        await GeneratePreschoolReportCardAsync(student, scores, academicYear, term, filePath, grade, academicYearInfo);
                        break;
                    case SchoolSection.Primary:
                        await GeneratePrimaryReportCardAsync(student, scores, academicYear, term, filePath, grade, academicYearInfo);
                        break;
                    case SchoolSection.Secondary:
                        await GenerateSecondaryReportCardAsync(student, scores, academicYear, term, filePath, grade, academicYearInfo);
                        break;
                }

                _logger.LogInformation($"Report card generated successfully: {fileName}");
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating report card for student {student.StudentNumber}");
                throw;
            }
        }

        private async Task GeneratePreschoolReportCardAsync(Student student, IEnumerable<ExamScore> scores,
            int academicYear, int term, string filePath, Grade grade, AcademicYear academicYearInfo)
        {
            await Task.Run(() =>
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(12));

                        page.Header()
                            .Height(100)
                            .Background(Colors.Blue.Lighten3)
                            .Padding(20)
                            .Row(row =>
                            {
                                row.RelativeItem().Column(column =>
                                {
                                    column.Item().AlignCenter().Text("CHUDLEIGH HOUSE SCHOOL").FontSize(24).Bold().FontColor(Colors.White);
                                    column.Item().AlignCenter().Text("PRESCHOOL REPORT CARD")
                                        .FontSize(16).FontColor(Colors.White);
                                });
                            });

                        page.Content()
                            .Padding(20)
                            .Column(column =>
                            {
                                // Student Information
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text($"Student Name: {student.FullName}").ExtraBold();
                                        col.Item().Text($"Student Number: {student.StudentNumber}");
                                        col.Item().Text($"Class: {grade?.FullName}");
                                    });
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text($"Academic Year: {academicYearInfo?.Name ?? academicYear.ToString()}");
                                        col.Item().Text($"Term: {term}");
                                        col.Item().Text($"Date of Birth: {student.DateOfBirth:dd/MM/yyyy}");
                                    });
                                });

                                column.Item().PaddingVertical(20).LineHorizontal(1);

                                // Preschool Assessment (Skill-based)
                                column.Item().Text("DEVELOPMENTAL ASSESSMENT").FontSize(16).Bold();

                                column.Item().PaddingTop(10).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(2);
                                    });

                                    // Header
                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("SKILL AREA").Bold();
                                        header.Cell().Element(CellStyle).Text("RATING").Bold();
                                        header.Cell().Element(CellStyle).Text("COMMENTS").Bold();
                                    });

                                    // Skills assessment
                                    var skillAreas = new[]
                                    {
                                        "Social Skills", "Language Development", "Fine Motor Skills",
                                        "Gross Motor Skills", "Cognitive Development", "Creative Expression"
                                    };

                                    foreach (var skill in skillAreas)
                                    {
                                        var rating = GetPreschoolRating();
                                        table.Cell().Element(CellStyle).Text(skill);
                                        table.Cell().Element(CellStyle).AlignCenter().Text(rating);
                                        table.Cell().Element(CellStyle).Text("Excellent progress shown");
                                    }
                                });

                                // Teacher Comments
                                column.Item().PaddingTop(20).Column(col =>
                                {
                                    col.Item().Text("TEACHER'S COMMENTS").FontSize(14).Bold();
                                    col.Item().PaddingTop(10).BorderHorizontal(1).BorderVertical(1)
                                        .Height(80).Padding(10)
                                        .Text($"{student.FirstName} has shown wonderful progress this term. " +
                                              "Continue encouraging learning through play and exploration.");
                                });

                                // Signature section
                                column.Item().PaddingTop(30).Row(row =>
                                {
                                    row.RelativeItem().Text($"Class Teacher: {grade?.HomeroomTeacher?.FullName ?? "________________"}");
                                    row.RelativeItem().AlignRight().Text($"Date: {DateTime.Now:dd/MM/yyyy}");
                                });
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Generated on ");
                                x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).Bold();
                            });
                    });
                }).GeneratePdf(filePath);
            });
        }

        private async Task GeneratePrimaryReportCardAsync(Student student, IEnumerable<ExamScore> scores,
            int academicYear, int term, string filePath, Grade grade, AcademicYear academicYearInfo)
        {
            await Task.Run(() =>
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header()
                            .Height(120)
                            .Background(Colors.Green.Lighten3)
                            .Padding(20)
                            .Row(row =>
                            {
                                row.RelativeItem().Column(column =>
                                {
                                    column.Item().AlignCenter().Text("CHUDLEIGH HOUSE SCHOOL")
                                        .FontSize(24).Bold().FontColor(Colors.White);
                                    column.Item().AlignCenter().Text("PRIMARY SCHOOL REPORT CARD")
                                        .FontSize(16).FontColor(Colors.White);
                                    column.Item().AlignCenter().Text($"TERM {term} - {academicYear}")
                                        .FontSize(12).FontColor(Colors.White);
                                });
                            });

                        page.Content()
                            .Padding(20)
                            .Column(column =>
                            {
                                // Calculate overall average first
                                var overallAverage = scores.Any() ? scores.Average(s => s.Score) : 0;

                                // Student Information
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text($"Student Name: {student.FullName}").Bold();
                                        col.Item().Text($"Student Number: {student.StudentNumber}");
                                        col.Item().Text($"Class: {grade?.FullName}");
                                    });
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text($"Academic Year: {academicYearInfo?.Name ?? academicYear.ToString()}");
                                        col.Item().Text($"Class Teacher: {grade?.HomeroomTeacher?.FullName ?? "Not Assigned"}");
                                        col.Item().Text($"Date of Birth: {student.DateOfBirth:dd/MM/yyyy}");
                                    });
                                });

                                column.Item().PaddingVertical(15).LineHorizontal(1);

                                // Academic Performance
                                column.Item().Text("ACADEMIC PERFORMANCE").FontSize(14).Bold();

                                column.Item().PaddingTop(10).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                    });

                                    // Header
                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("SUBJECT").Bold();
                                        header.Cell().Element(CellStyle).Text("TEST 1").Bold();
                                        header.Cell().Element(CellStyle).Text("MID-TERM").Bold();
                                        header.Cell().Element(CellStyle).Text("END TERM").Bold();
                                        header.Cell().Element(CellStyle).Text("AVERAGE").Bold();
                                        header.Cell().Element(CellStyle).Text("GRADE").Bold();
                                    });

                                    // Subject scores
                                    var subjectGroups = scores.GroupBy(s => s.Subject);
                                    foreach (var subjectGroup in subjectGroups)
                                    {
                                        var subjectScores = subjectGroup.ToList();
                                        var test1 = subjectScores.FirstOrDefault(s => s.ExamType.Name == "Test-One")?.Score ?? 0;
                                        var midTerm = subjectScores.FirstOrDefault(s => s.ExamType.Name == "Mid-Term")?.Score ?? 0;
                                        var endTerm = subjectScores.FirstOrDefault(s => s.ExamType.Name == "End-of-Term")?.Score ?? 0;

                                        var average = (test1 + midTerm + endTerm) / 3;
                                        var grade = GetGrade(average);

                                        table.Cell().Element(CellStyle).Text(subjectGroup.Key.Name);
                                        table.Cell().Element(CellStyle).AlignCenter().Text(test1.ToString("F0"));
                                        table.Cell().Element(CellStyle).AlignCenter().Text(midTerm.ToString("F0"));
                                        table.Cell().Element(CellStyle).AlignCenter().Text(endTerm.ToString("F0"));
                                        table.Cell().Element(CellStyle).AlignCenter().Text(average.ToString("F1"));
                                        table.Cell().Element(CellStyle).AlignCenter().Text(grade).Bold();
                                    }

                                    // Overall average row
                                    table.Cell().Element(CellStyle).Text("OVERALL AVERAGE").Bold();
                                    table.Cell().Element(CellStyle).Text("");
                                    table.Cell().Element(CellStyle).Text("");
                                    table.Cell().Element(CellStyle).Text("");
                                    table.Cell().Element(CellStyle).AlignCenter().Text(overallAverage.ToString("F1")).Bold();
                                    table.Cell().Element(CellStyle).AlignCenter().Text(GetGrade(overallAverage)).Bold();
                                });

                                // Grading Scale
                                column.Item().PaddingTop(15).Row(row =>
                                {
                                    row.RelativeItem().Text("GRADING SCALE: A (80-100) | B (70-79) | C (60-69) | D (50-59) | F (0-49)")
                                        .FontSize(8).Italic();
                                });

                                // Teacher Comments
                                column.Item().PaddingTop(15).Column(col =>
                                {
                                    col.Item().Text("TEACHER'S COMMENTS").FontSize(12).Bold();
                                    col.Item().PaddingTop(5).BorderHorizontal(1).BorderVertical(1)
                                        .Height(60).Padding(8)
                                        .Text(GenerateTeacherComment(overallAverage, student.FirstName));
                                });

                                // Signature section
                                column.Item().PaddingTop(20).Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("Class Teacher: ________________");
                                        col.Item().PaddingTop(5).Text($"Name: {grade?.HomeroomTeacher?.FullName ?? "Not Assigned"}");
                                    });
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("Head Teacher: ________________");
                                        col.Item().PaddingTop(5).Text("Date: ________________");
                                    });
                                });
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Generated on ");
                                x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).Bold();
                            });
                    });
                }).GeneratePdf(filePath);
            });
        }

        private async Task GenerateSecondaryReportCardAsync(Student student, IEnumerable<ExamScore> scores,
            int academicYear, int term, string filePath, Grade grade, AcademicYear academicYearInfo)
        {
            await Task.Run(() =>
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(9));

                        page.Header()
                            .Height(130)
                            .Background(Colors.Blue.Darken2)
                            .Padding(15)
                            .Row(row =>
                            {
                                row.RelativeItem().Column(column =>
                                {
                                    column.Item().AlignCenter().Text("CHUDLEIGH HOUSE SCHOOL")
                                        .FontSize(26).Bold().FontColor(Colors.White);
                                    column.Item().AlignCenter().Text("SECONDARY SCHOOL REPORT CARD")
                                        .FontSize(16).FontColor(Colors.White);
                                    column.Item().AlignCenter().Text($"TERM {term} EXAMINATION - {academicYear}")
                                        .FontSize(12).FontColor(Colors.White);
                                });
                            });

                        page.Content()
                            .Padding(15)
                            .Column(column =>
                            {
                                // Student Information Header
                                column.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text($"NAME: {student.FullName}").Bold();
                                        col.Item().Text($"STUDENT ID: {student.StudentNumber}");
                                    });
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text($"CLASS: {grade?.FullName}").Bold();
                                        col.Item().Text($"ACADEMIC YEAR: {academicYearInfo?.Name ?? academicYear.ToString()}");
                                    });
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text($"TERM: {term}").Bold();
                                        col.Item().Text($"DATE OF BIRTH: {student.DateOfBirth:dd/MM/yyyy}");
                                    });
                                });

                                column.Item().PaddingVertical(10).LineHorizontal(1);

                                // Academic Performance Table
                                column.Item().Text("ACADEMIC PERFORMANCE").FontSize(14).Bold();

                                // Calculate average points for comments
                                decimal totalPoints = 0;
                                int subjectCount = 0;

                                column.Item().PaddingTop(8).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                    });

                                    // Header
                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("SUBJECT").Bold();
                                        header.Cell().Element(CellStyle).Text("TEST 1").Bold();
                                        header.Cell().Element(CellStyle).Text("MID-TERM").Bold();
                                        header.Cell().Element(CellStyle).Text("END TERM").Bold();
                                        header.Cell().Element(CellStyle).Text("TOTAL").Bold();
                                        header.Cell().Element(CellStyle).Text("GRADE").Bold();
                                        header.Cell().Element(CellStyle).Text("POINTS").Bold();
                                        header.Cell().Element(CellStyle).Text("REMARK").Bold();
                                    });

                                    // Subject scores
                                    var subjectGroups = scores.GroupBy(s => s.Subject);

                                    foreach (var subjectGroup in subjectGroups)
                                    {
                                        var subjectScores = subjectGroup.ToList();
                                        var test1 = subjectScores.FirstOrDefault(s => s.ExamType.Name == "Test-One")?.Score ?? 0;
                                        var midTerm = subjectScores.FirstOrDefault(s => s.ExamType.Name == "Mid-Term")?.Score ?? 0;
                                        var endTerm = subjectScores.FirstOrDefault(s => s.ExamType.Name == "End-of-Term")?.Score ?? 0;

                                        var total = test1 + midTerm + endTerm;
                                        var grade = GetSecondaryGrade(total);
                                        var points = GetGradePoints(grade);
                                        var remark = GetRemark(grade);

                                        totalPoints += points;
                                        subjectCount++;

                                        table.Cell().Element(CellStyle).Text(subjectGroup.Key.Name);
                                        table.Cell().Element(CellStyle).AlignCenter().Text(test1.ToString("F0"));
                                        table.Cell().Element(CellStyle).AlignCenter().Text(midTerm.ToString("F0"));
                                        table.Cell().Element(CellStyle).AlignCenter().Text(endTerm.ToString("F0"));
                                        table.Cell().Element(CellStyle).AlignCenter().Text(total.ToString("F0"));
                                        table.Cell().Element(CellStyle).AlignCenter().Text(grade).Bold();
                                        table.Cell().Element(CellStyle).AlignCenter().Text(points.ToString("F0"));
                                        table.Cell().Element(CellStyle).AlignCenter().Text(remark);
                                    }

                                    // Summary row
                                    var averagePoints = subjectCount > 0 ? totalPoints / subjectCount : 0;
                                    table.Cell().Element(CellStyle).Text("TOTAL/AVERAGE").Bold();
                                    table.Cell().Element(CellStyle).Text("");
                                    table.Cell().Element(CellStyle).Text("");
                                    table.Cell().Element(CellStyle).Text("");
                                    table.Cell().Element(CellStyle).Text("");
                                    table.Cell().Element(CellStyle).Text("");
                                    table.Cell().Element(CellStyle).AlignCenter().Text(averagePoints.ToString("F1")).Bold();
                                    table.Cell().Element(CellStyle).AlignCenter().Text(GetOverallRemark(averagePoints)).Bold();
                                });

                                // Calculate final average points for comments outside the table
                                var finalAveragePoints = subjectCount > 0 ? totalPoints / subjectCount : 0;

                                // Class Performance Statistics
                                column.Item().PaddingTop(10).Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("CLASS STATISTICS").Bold();
                                        col.Item().Text($"Class Size: 45");
                                        col.Item().Text($"Position in Class: {new Random().Next(1, 46)}");
                                    });
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("GRADING SYSTEM").Bold();
                                        col.Item().Text("A (12pts) | B (9pts) | C (6pts) | D (3pts) | F (0pts)").FontSize(8);
                                    });
                                });

                                // Teacher and Head Comments
                                column.Item().PaddingTop(10).Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("CLASS TEACHER'S COMMENTS").FontSize(10).Bold();
                                        col.Item().PaddingTop(3).BorderHorizontal(1).BorderVertical(1)
                                            .Height(50).Padding(5)
                                            .Text(GenerateSecondaryTeacherComment(finalAveragePoints, student.FirstName));
                                    });
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("HEAD TEACHER'S COMMENTS").FontSize(10).Bold();
                                        col.Item().PaddingTop(3).BorderHorizontal(1).BorderVertical(1)
                                            .Height(50).Padding(5)
                                            .Text(GenerateHeadTeacherComment(finalAveragePoints));
                                    });
                                });

                                // Signature section
                                column.Item().PaddingTop(15).Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("Class Teacher: ________________");
                                        col.Item().PaddingTop(3).Text($"Name: {grade?.HomeroomTeacher?.FullName ?? "Not Assigned"}");
                                    });
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("Head Teacher: ________________");
                                        col.Item().PaddingTop(3).Text("Date: ________________");
                                    });
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("Parent/Guardian: ________________");
                                        col.Item().PaddingTop(3).Text("Date: ________________");
                                    });
                                });
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Generated on ");
                                x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).Bold();
                            });
                    });
                }).GeneratePdf(filePath);
            });
        }

        // Helper methods for PDF generation
        private static IContainer CellStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Medium)
                .Padding(5)
                .AlignMiddle();
        }

        private static string GetPreschoolRating()
        {
            var ratings = new[] { "★★★★★", "★★★★☆", "★★★☆☆", "★★☆☆☆" };
            return ratings[new Random().Next(ratings.Length)];
        }

        private static string GetGrade(decimal score)
        {
            if (score >= 80) return "A";
            if (score >= 70) return "B";
            if (score >= 60) return "C";
            if (score >= 50) return "D";
            return "F";
        }

        private static string GetSecondaryGrade(decimal totalScore)
        {
            if (totalScore >= 240) return "A"; // 80% average
            if (totalScore >= 210) return "B"; // 70% average
            if (totalScore >= 180) return "C"; // 60% average
            if (totalScore >= 150) return "D"; // 50% average
            return "F";
        }

        private static decimal GetGradePoints(string grade)
        {
            return grade switch
            {
                "A" => 12,
                "B" => 9,
                "C" => 6,
                "D" => 3,
                _ => 0
            };
        }

        private static string GetRemark(string grade)
        {
            return grade switch
            {
                "A" => "Excellent",
                "B" => "Very Good",
                "C" => "Good",
                "D" => "Satisfactory",
                _ => "Needs Improvement"
            };
        }

        private static string GetOverallRemark(decimal averagePoints)
        {
            if (averagePoints >= 10) return "EXCELLENT";
            if (averagePoints >= 8) return "VERY GOOD";
            if (averagePoints >= 6) return "GOOD";
            if (averagePoints >= 4) return "SATISFACTORY";
            return "NEEDS IMPROVEMENT";
        }

        private static string GenerateTeacherComment(decimal average, string firstName)
        {
            if (average >= 80)
                return $"{firstName} has demonstrated excellent academic performance this term. Continue with the same dedication and effort.";
            if (average >= 70)
                return $"{firstName} has shown very good progress. With continued effort, even better results can be achieved.";
            if (average >= 60)
                return $"{firstName} has made good progress this term. Focus on areas that need improvement for better results.";
            if (average >= 50)
                return $"{firstName} has shown satisfactory performance. More effort and dedication are needed to improve grades.";
            return $"{firstName} needs significant improvement in academic performance. Extra support and study time are recommended.";
        }

        private static string GenerateSecondaryTeacherComment(decimal averagePoints, string firstName)
        {
            if (averagePoints >= 10)
                return $"{firstName} has excelled academically this term. Maintain this excellent standard.";
            if (averagePoints >= 8)
                return $"{firstName} has performed very well. Continue working hard to achieve excellence.";
            if (averagePoints >= 6)
                return $"{firstName} has shown good progress. Focus on weaker subjects for improvement.";
            if (averagePoints >= 4)
                return $"{firstName} needs to put in more effort to improve academic performance.";
            return $"{firstName} requires immediate attention and extra support to meet academic standards.";
        }

        private static string GenerateHeadTeacherComment(decimal averagePoints)
        {
            if (averagePoints >= 10)
                return "Outstanding performance. Keep up the excellent work.";
            if (averagePoints >= 8)
                return "Very commendable effort. Strive for excellence.";
            if (averagePoints >= 6)
                return "Good effort shown. Work harder for better results.";
            if (averagePoints >= 4)
                return "Satisfactory performance. More dedication needed.";
            return "Immediate improvement required. Seek additional support.";
        }
    }
}