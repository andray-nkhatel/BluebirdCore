// ===== DYNAMIC 4-PAGE QUESTPDF REPORT CARD SERVICE =====
// Integrated with Vue Score Entry Component Data Structure

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using BluebirdCore.Entities;
using Microsoft.EntityFrameworkCore;
using BluebirdCore.Data;
using QuestPDF.Previewer;

namespace BluebirdCore.Services
{
    public class ReportCardPdfService
    {
        // private readonly string _reportPath;
        private readonly SchoolDbContext _context;
        private readonly ILogger<ReportCardPdfService> _logger;

        public ReportCardPdfService(IConfiguration configuration, SchoolDbContext context, ILogger<ReportCardPdfService> logger)
        {
            // _reportPath = configuration["ReportCards:StoragePath"] ?? "Reports";
            _context = context;
            _logger = logger;
            // Directory.CreateDirectory(_reportPath);

            // Configure QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateReportCardPdfAsync(int studentId, int academicYearId, int term)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.Grade)
                        .ThenInclude(g => g.HomeroomTeacher)
                    .FirstOrDefaultAsync(s => s.Id == studentId);

                if (student == null)
                    throw new ArgumentException($"Student with ID {studentId} not found");

                var academicYear = await _context.AcademicYears
                    .FirstOrDefaultAsync(ay => ay.Id == academicYearId);

                if (academicYear == null)
                    throw new ArgumentException($"Academic year with ID {academicYearId} not found");

                var examScores = await GetStudentExamScores(studentId, academicYearId, term);

                switch (student.Grade.Section)
                {
                    case SchoolSection.Preschool:
                        return await GeneratePreschoolReportCardAsync(student, examScores, academicYear, term);
                    case SchoolSection.Primary:
                        return await GeneratePrimaryReportCardAsync(student, examScores, academicYear, term);
                    case SchoolSection.Secondary:
                        return await GenerateSecondaryReportCardAsync(student, examScores, academicYear, term);
                    default:
                        throw new InvalidOperationException("Unknown school section");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating report card for student {studentId}");
                throw;
            }
        }

        public async Task<byte[]> GenerateReportCardPdfAsync(Student student, IEnumerable<ExamScore> scores, int academicYear, int term)
        {
            var academicYearEntity = await _context.AcademicYears
                .FirstOrDefaultAsync(ay => ay.Name.Contains(academicYear.ToString()));

            if (academicYearEntity == null)
                throw new ArgumentException($"Academic year {academicYear} not found");

            return await GenerateReportCardPdfAsync(student.Id, academicYearEntity.Id, term);
        }

        private async Task<List<StudentExamData>> GetStudentExamScores(int studentId, int academicYearId, int term)
        {
            var scores = await _context.ExamScores
                .Include(es => es.Subject)
                .Include(es => es.ExamType)
                .Include(es => es.RecordedByTeacher)
                .Include(es => es.CommentsUpdatedByTeacher)
                .Where(es => es.StudentId == studentId && 
                           es.AcademicYear == academicYearId && 
                           es.Term == term)
                .ToListAsync();

            var subjectGroups = scores.GroupBy(s => s.Subject);
            var examData = new List<StudentExamData>();

            foreach (var subjectGroup in subjectGroups)
            {
                var subject = subjectGroup.Key;
                var subjectScores = subjectGroup.ToList();

                var test1Score = subjectScores.FirstOrDefault(s => s.ExamType.Name == "Test-One");
                var midTermScore = subjectScores.FirstOrDefault(s => s.ExamType.Name == "Mid-Term");
                var endTermScore = subjectScores.FirstOrDefault(s => s.ExamType.Name == "End-of-Term");

                var comments = endTermScore?.Comments;

                examData.Add(new StudentExamData
                {
                    SubjectId = subject.Id,
                    SubjectName = subject.Name,
                    Test1Score = test1Score?.Score ?? 0,
                    MidTermScore = midTermScore?.Score ?? 0,
                    EndTermScore = endTermScore?.Score ?? 0,
                    Comments = comments,
                    CommentsUpdatedAt = endTermScore?.CommentsUpdatedAt,
                    CommentsUpdatedBy = endTermScore?.CommentsUpdatedByTeacher?.FullName,
                    LastUpdated = endTermScore?.RecordedAt ?? DateTime.Now,
                    RecordedBy = endTermScore?.RecordedByTeacher?.FullName
                });
            }

            return examData.OrderBy(e => e.SubjectName).ToList();
        }



        

        
        
        

        private async Task<byte[]> GeneratePreschoolReportCardAsync(Student student, List<StudentExamData> examData,
            AcademicYear academicYear, int term)
        {
            return await Task.Run(() =>
            {
                using var ms = new MemoryStream();

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                        {
                            ConfigureBasicPage(page);




                            // Remove page.Header() and put everything in Content
                            page.Content()
                            .Border(5)
                            .BorderColor(Colors.Blue.Darken2)
                            .Column(column =>
                            {
                                // Header section (only on page 1)
                                column.Item()
                                .PaddingTop(20)
                                .PaddingBottom(5)
                                .PaddingLeft(20)
                                .PaddingRight(20)
                                .Column(headerColumn =>
                                {
                                    headerColumn.Item().Text("CHUDLEIGH HOUSE SCHOOL").FontSize(18).Bold().AlignCenter();
                                    headerColumn.Item().Text("PCELC REPORT CARD").FontSize(14).AlignCenter();
                                    headerColumn.Item().PaddingTop(10).LineHorizontal(1);
                                    AddStudentInformation(headerColumn, student, academicYear, term);
                                    headerColumn.Item().PaddingTop(10).LineHorizontal(1);
                                    headerColumn.Item().PaddingTop(20).Text("DEVELOPMENTAL PERFORMANCE").FontSize(14).Bold();
                                });

                                // Content section
                                column.Item()
                                .PaddingTop(10)
                                .PaddingLeft(10)
                                .PaddingRight(10)
                                .PaddingBottom(10)
                                .Column(contentColumn =>
                                {
                                    // Full table - will auto-break across pages
                                    AddPrimaryScoreTable(contentColumn, examData, true);

                                    contentColumn.Item().PaddingTop(20).LineHorizontal(1);
                                    contentColumn.Item().PageBreak();
                                    contentColumn.Item().PaddingTop(10).Text("Class Teacher's General Comment:").FontSize(14).Bold();

                                    // Generate the teacher assessment
                                    var teacherComment = GenerateTeacherAssessment(examData, student.FirstName);

                                    // Use homeroom teacher for general comments
                                    var generalCommentTeacher = student.Grade?.HomeroomTeacher?.FullName ?? "Class Teacher";

                                    contentColumn.Item().PaddingTop(10).MinHeight(80)
                                        .Background(Colors.Grey.Lighten3)
                                        .Padding(10)
                                        .Text($"{generalCommentTeacher}:\n\n{teacherComment}")
                                        .FontSize(11)
                                        .LineHeight(1.3f);
                                });
                            });
                        });

                    // PAGE 2: Administrative Section & Grading Scale (without header)
                    container.Page(page =>
                    {
                        ConfigureBasicPage(page);

                        page.Content().Border(5).BorderColor(Colors.Blue.Darken2).Padding(20).Column(column =>
                        {
                            column.Item().PaddingTop(5).LineHorizontal(1);

                            var overallAverage = CalculateOverallAverage(examData);

                            AddPreschoolAdministrativeSection(column, student.Grade);
                            //AddPrimaryGradingScale(column);

                            column.Item().PaddingTop(170).Column(contact =>
                            {
                                contact.Item().Text("Chudleigh House School").FontSize(14).Bold().AlignCenter();
                                contact.Item().Text("Plot 11289, Lusaka, Zambia").FontSize(8).AlignCenter();
                                contact.Item().Text("Tel: +260-955-876333  | +260-953-074465").FontSize(12).AlignCenter().FontSize(8);
                                contact.Item().Text("Email: info@chudleighhouseschool.com").AlignCenter().FontSize(8);
                                contact.Item().Text("Website: www.chudleighhouseschool.com").FontSize(8).AlignCenter();
                            });
                        });
                    });

                    // PAGE 3: Cover Page (without header)
                    AddCoverPage(container, student, academicYear, term, "PCELC");

                });
                document.GeneratePdf(ms);
                return ms.ToArray();
            });
        }

        private async Task<byte[]> GeneratePrimaryReportCardAsync(Student student, List<StudentExamData> examData,
            AcademicYear academicYear, int term)
        {
            return await Task.Run(() =>
            {
                var halfCount = (examData.Count + 1) / 2;
                try
                {
                    using var ms = new MemoryStream();
                    Document.Create(container =>
                    {
                        // PAGE 1: Student Information & Scores (Part 1)
                        container.Page(page =>
                        {
                            ConfigureBasicPage(page);

                            // Remove page.Header() and put everything in Content
                            page.Content()
                            .Border(5)
                            .BorderColor(Colors.Blue.Darken2)
                            .Column(column =>
                            {
                                // Header section (only on page 1)
                                column.Item()
                                .PaddingTop(20)
                                .PaddingBottom(5)
                                .PaddingLeft(20)
                                .PaddingRight(20)
                                .Column(headerColumn =>
                                {
                                    headerColumn.Item().Text("CHUDLEIGH HOUSE SCHOOL").FontSize(18).Bold().AlignCenter();
                                    headerColumn.Item().Text("PRIMARY SCHOOL REPORT CARD").FontSize(14).AlignCenter();
                                    headerColumn.Item().PaddingTop(10).LineHorizontal(1);
                                    AddStudentInformation(headerColumn, student, academicYear, term);
                                    headerColumn.Item().PaddingTop(10).LineHorizontal(1);
                                    headerColumn.Item().PaddingTop(20).Text("ACADEMIC PERFORMANCE").FontSize(14).Bold();
                                });

                                // Content section
                                column.Item()
                                .PaddingTop(10)
                                .PaddingLeft(10)
                                .PaddingRight(10)
                                .PaddingBottom(10)
                                .Column(contentColumn =>
                                {
                                    // Full table - will auto-break across pages
                                    AddPrimaryScoreTable(contentColumn, examData, true);

                                    contentColumn.Item().PaddingTop(20).LineHorizontal(1);
                                    contentColumn.Item().PageBreak();
                                    contentColumn.Item().PaddingTop(10).Text("Class Teacher's General Comment:").FontSize(14).Bold();

                                    // Generate the teacher assessment
                                    var teacherComment = GenerateTeacherAssessment(examData, student.FirstName);

                                    // Use homeroom teacher for general comments
                                    var generalCommentTeacher = student.Grade?.HomeroomTeacher?.FullName ?? "Class Teacher";

                                    contentColumn.Item().PaddingTop(10).MinHeight(80)
                                        .Background(Colors.Grey.Lighten3)
                                        .Padding(10)
                                        .Text($"{generalCommentTeacher}:\n\n{teacherComment}")
                                        .FontSize(11)
                                        .LineHeight(1.3f);
                                });
                            });
                        });

                        // PAGE 2: Administrative Section & Grading Scale (without header)
                        container.Page(page =>
                        {
                            ConfigureBasicPage(page);

                            page.Content().Border(5).BorderColor(Colors.Blue.Darken2).Padding(20).Column(column =>
                            {
                                column.Item().PaddingTop(5).LineHorizontal(1);

                                var overallAverage = CalculateOverallAverage(examData);

                                AddPrimaryAdministrativeSection(column, student.Grade);
                                AddPrimaryGradingScale(column);

                                column.Item().PaddingTop(170).Column(contact =>
                                {
                                    contact.Item().Text("Chudleigh House School").FontSize(14).Bold().AlignCenter();
                                    contact.Item().Text("Plot 11289, Lusaka, Zambia").FontSize(8).AlignCenter();
                                    contact.Item().Text("Tel: +260-955-876333  | +260-953-074465").FontSize(12).AlignCenter().FontSize(8);
                                    contact.Item().Text("Email: info@chudleighhouseschool.com").AlignCenter().FontSize(8);
                                    contact.Item().Text("Website: www.chudleighhouseschool.com").FontSize(8).AlignCenter();
                                });
                            });
                        });

                        // PAGE 3: Cover Page (without header)
                        AddCoverPage(container, student, academicYear, term, "PRIMARY SCHOOL");

                    }).GeneratePdf(ms);
                    _logger.LogInformation("PDF generated successfully");
                    return ms.ToArray();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"PDF generation failed: {ex.Message}");
                    throw;
                }
            });
        }
        private async Task<byte[]> GenerateSecondaryReportCardAsync(Student student, List<StudentExamData> examData,
            AcademicYear academicYear, int term)
        {
            return await Task.Run(() =>
            {
                var halfCount = (examData.Count + 1) / 2;
                using var ms = new MemoryStream();
                Document.Create(container =>
                {
                    // PAGE 1: Student Information & Scores (Part 1)

                    container.Page(page =>
                        {
                            ConfigureBasicPage(page);

                            // Remove page.Header() and put everything in Content
                            page.Content()
                            .Border(5)
                            .BorderColor(Colors.Blue.Darken2)
                            .Column(column =>
                            {
                                // Header section (only on page 1)
                                column.Item()
                                .PaddingTop(20)
                                .PaddingBottom(5)
                                .PaddingLeft(20)
                                .PaddingRight(20)
                                .Column(headerColumn =>
                                {
                                    headerColumn.Item().Text("CHUDLEIGH HOUSE SCHOOL").FontSize(18).Bold().AlignCenter();
                                    headerColumn.Item().Text("SECONDARY SCHOOL REPORT CARD").FontSize(14).AlignCenter();
                                    headerColumn.Item().PaddingTop(10).LineHorizontal(1);
                                    AddStudentInformation(headerColumn, student, academicYear, term);
                                    headerColumn.Item().PaddingTop(10).LineHorizontal(1);
                                    headerColumn.Item().PaddingTop(20).Text("ACADEMIC PERFORMANCE").FontSize(14).Bold();
                                });

                                // Content section
                                column.Item()
                                .PaddingTop(10)
                                .PaddingLeft(10)
                                .PaddingRight(10)
                                .PaddingBottom(10)
                                .Column(contentColumn =>
                                {
                                    // Full table - will auto-break across pages
                                    AddSecondaryScoreTable(contentColumn, examData, true);

                                    contentColumn.Item().PaddingTop(20).LineHorizontal(1);
                                    contentColumn.Item().PageBreak();
                                    contentColumn.Item().PaddingTop(10).Text("Class Teacher's General Comment:").FontSize(14).Bold();

                                    // Generate the teacher assessment
                                    var teacherComment = GenerateTeacherAssessment(examData, student.FirstName);

                                    // Use homeroom teacher for general comments
                                    var generalCommentTeacher = student.Grade?.HomeroomTeacher?.FullName ?? "Class Teacher";

                                    contentColumn.Item().PaddingTop(10).MinHeight(80)
                                        .Background(Colors.Grey.Lighten3)
                                        .Padding(10)
                                        .Text($"{generalCommentTeacher}:\n\n{teacherComment}")
                                        .FontSize(11)
                                        .LineHeight(1.3f);
                                });
                            });
                        });


                    // PAGE 3: Teacher Comments & Administrative

                    container.Page(page =>
                    {
                        ConfigureBasicPage(page);

                        page.Content().Border(5).BorderColor(Colors.Blue.Darken2).Padding(20).Column(column =>
                        {
                            column.Item().PaddingTop(5).LineHorizontal(1);

                            var overallAverage = CalculateOverallAverage(examData);

                            AddSecondaryAdministrativeSection(column, student.Grade);
                            AddSecondaryGradingScale(column);

                            column.Item().PaddingTop(170).Column(contact =>
                            {
                                contact.Item().Text("Chudleigh House School").FontSize(14).Bold().AlignCenter();
                                contact.Item().Text("Plot 11289, Lusaka, Zambia").FontSize(8).AlignCenter();
                                contact.Item().Text("Tel: +260-955-876333  | +260-953-074465").FontSize(12).AlignCenter().FontSize(8);
                                contact.Item().Text("Email: info@chudleighhouseschool.com").AlignCenter().FontSize(8);
                                contact.Item().Text("Website: www.chudleighhouseschool.com").FontSize(8).AlignCenter();
                            });
                        });
                    });

                    // PAGE 4: Cover Page
                    AddCoverPage(container, student, academicYear, term, "SECONDARY SCHOOL");

                }).GeneratePdf(ms);
                return ms.ToArray();
            });
        }

        // Helper Methods
        private static void ConfigureBasicPage(PageDescriptor page)
        {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(10));


            page.Background()
            .AlignCenter()
            .AlignMiddle()
            .Width(500)
            .Image("./Media/chs-wm-logo.png");
        }

        private static void AddStudentInformation(ColumnDescriptor column, Student student, 
            AcademicYear academicYear, int term)
        {
            column.Item().PaddingTop(5).Text("STUDENT INFORMATION").FontSize(14).Bold();
            
            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Name: {student.FirstName} {student.LastName}").Bold();
                    col.Item().Text($"Year: {academicYear?.Name}").Bold();
                    //col.Item().Text($"Next Term Begins: {DateTime.Now.AddDays(30):dd/MM/yyyy}").Bold();
                    //col.Item().Text($"Student Number: {student.StudentNumber}");
                    //col.Item().Text($"Date of Birth: {student.DateOfBirth:dd/MM/yyyy}");
                });
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Class: {student.Grade?.FullName}").Bold();
                    col.Item().Text($"Term: {term}").Bold();
                });
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Class Teacher: {student.Grade?.HomeroomTeacher?.FullName ?? "Not Assigned"}").Bold();
                    col.Item().Text($"Report Date: {DateTime.Now:dd-MM-yyyy}").Bold();
                    
                });
            });
        }

        private static void AddPrimaryScoreTable(ColumnDescriptor column, List<StudentExamData> examData, bool showHeader)
        {
            column.Item().Table(table =>
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

                if (showHeader)
                {
                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("SUBJECT").Bold();
                        header.Cell().Element(CellStyle).Text("TEST 1").Bold();
                        header.Cell().Element(CellStyle).Text("TEST 2").Bold();
                        header.Cell().Element(CellStyle).Text("END TERM").Bold();
                        header.Cell().Element(CellStyle).Text("AVERAGE").Bold();
                        header.Cell().Element(CellStyle).Text("GRADE").Bold();
                    });
                }

                foreach (var exam in examData)
                {
                    var average = (exam.Test1Score + exam.MidTermScore + exam.EndTermScore) / 3;
                    var grade = GetGrade(average);

                    table.Cell().Background(Colors.Grey.Lighten2).Element(CellStyle).Text(exam.SubjectName);
                    table.Cell().Background(Colors.Grey.Lighten2).Element(CellStyle).AlignCenter().Text(exam.Test1Score.ToString("F0"));
                    table.Cell().Background(Colors.Grey.Lighten2).Element(CellStyle).AlignCenter().Text(exam.MidTermScore.ToString("F0"));
                    table.Cell().Background(Colors.Grey.Lighten2).Element(CellStyle).AlignCenter().Text(exam.EndTermScore.ToString("F0"));
                    table.Cell().Background(Colors.Grey.Lighten2).Element(CellStyle).AlignCenter().Text(average.ToString("F1"));
                    table.Cell().Background(Colors.Grey.Lighten2).Element(CellStyle).AlignCenter().Text(grade).Bold();

                    // Second row: Comments (if available)
                    if (!string.IsNullOrWhiteSpace(exam.Comments))
                    {
                        var commentAuthor = !string.IsNullOrEmpty(exam.CommentsUpdatedBy)
                                          ? exam.CommentsUpdatedBy
                                          : exam.RecordedBy ?? "System";

                        table.Cell().Element(CellStyle).Text($"Remark:");
                        table.Cell().ColumnSpan(5).Element(CellStyle).Column(commentCol =>
                        {

                            // Truncate long comments
                            var truncatedComment = exam.Comments.Length > 100 ?
                                exam.Comments.Substring(0, 100) + "..." : exam.Comments;
                            commentCol.Item().Text(truncatedComment).FontSize(8).FontColor(Colors.Black);

                           
                        });
                        
                        
                    }
                        
                       
                }
            });
        }

        

        private static void AddSecondaryScoreTable(ColumnDescriptor column, List<StudentExamData> examData, bool showHeader)
        {
           column.Item().Table(table =>
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

                if (showHeader)
                {
                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("SUBJECT").Bold();
                        header.Cell().Element(CellStyle).Text("TEST 1").Bold();
                        header.Cell().Element(CellStyle).Text("TEST 2").Bold();
                        header.Cell().Element(CellStyle).Text("END TERM").Bold();
                        header.Cell().Element(CellStyle).Text("AVERAGE").Bold();
                        header.Cell().Element(CellStyle).Text("GRADE").Bold();
                    });
                }

                foreach (var exam in examData)
                {
                    var average = (exam.Test1Score + exam.MidTermScore + exam.EndTermScore) / 3;
                    var grade = GetGrade(average);

                    table.Cell().Background(Colors.Grey.Lighten2).Element(CellStyle).Text(exam.SubjectName);
                    table.Cell().Background(Colors.Grey.Lighten2).Element(CellStyle).AlignCenter().Text(exam.Test1Score.ToString("F0"));
                    table.Cell().Background(Colors.Grey.Lighten2).Element(CellStyle).AlignCenter().Text(exam.MidTermScore.ToString("F0"));
                    table.Cell().Background(Colors.Grey.Lighten2).Element(CellStyle).AlignCenter().Text(exam.EndTermScore.ToString("F0"));
                    table.Cell().Background(Colors.Grey.Lighten2).Element(CellStyle).AlignCenter().Text(average.ToString("F1"));
                    table.Cell().Background(Colors.Grey.Lighten2).Element(CellStyle).AlignCenter().Text(grade).Bold();

                    // Second row: Comments (if available)
                    if (!string.IsNullOrWhiteSpace(exam.Comments))
                    {
                        var commentAuthor = !string.IsNullOrEmpty(exam.CommentsUpdatedBy)
                                          ? exam.CommentsUpdatedBy
                                          : exam.RecordedBy ?? "System";

                        table.Cell().Element(CellStyle).Text($"Remark:");
                        table.Cell().ColumnSpan(5).Element(CellStyle).Column(commentCol =>
                        {

                            // Truncate long comments
                            var truncatedComment = exam.Comments.Length > 100 ?
                                exam.Comments.Substring(0, 100) + "..." : exam.Comments;
                            commentCol.Item().Text(truncatedComment).FontSize(8).FontColor(Colors.Black);

                           
                        });
                        
                        
                    }
                        
                       
                }
            });
        }

        private static void AddEndOfTermCommentsSection(ColumnDescriptor column, List<StudentExamData> examData, string part)
        {
            var subjectsWithComments = examData.Where(e => !string.IsNullOrWhiteSpace(e.Comments)).ToList();
            
            if (subjectsWithComments.Any())
            {
                column.Item().PaddingTop(15).Text($"END-OF-TERM COMMENTS - {part}").FontSize(12).Bold();
                
                foreach (var exam in subjectsWithComments)
                {
                    column.Item().PaddingTop(5).Column(col =>
                    {
                        col.Item().Text($"{exam.SubjectName}:").FontSize(10).Bold();
                        col.Item().Text(exam.Comments).FontSize(9);
                        if (exam.CommentsUpdatedAt.HasValue)
                        {
                            col.Item().Text($"Updated: {exam.CommentsUpdatedAt:dd/MM/yyyy} by {exam.CommentsUpdatedBy}")
                                .FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                        }
                    });
                }
            }
        }

        private static void AddOverallSummary(ColumnDescriptor column, List<StudentExamData> examData)
        {
            var overallAverage = CalculateOverallAverage(examData);
            
            column.Item().PaddingTop(15).Table(table =>
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

                table.Cell().Element(CellStyle).Text("OVERALL AVERAGE").Bold();
                table.Cell().Element(CellStyle).Text("");
                table.Cell().Element(CellStyle).Text("");
                table.Cell().Element(CellStyle).Text("");
                table.Cell().Element(CellStyle).AlignCenter().Text(overallAverage.ToString("F1")).Bold();
                table.Cell().Element(CellStyle).AlignCenter().Text(GetGrade(overallAverage)).Bold();
            });
        }

        private static void AddSecondaryOverallSummary(ColumnDescriptor column, List<StudentExamData> examData)
        {
            var averagePoints = CalculateAveragePoints(examData);
            
            column.Item().PaddingTop(15).Table(table =>
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

                table.Cell().Element(CellStyle).Text("TOTAL/AVERAGE").Bold();
                table.Cell().Element(CellStyle).Text("");
                table.Cell().Element(CellStyle).Text("");
                table.Cell().Element(CellStyle).Text("");
                table.Cell().Element(CellStyle).Text("");
                table.Cell().Element(CellStyle).Text("");
                table.Cell().Element(CellStyle).AlignCenter().Text(averagePoints.ToString("F1")).Bold();
                table.Cell().Element(CellStyle).AlignCenter().Text(GetOverallRemark(averagePoints)).Bold();
            });
        }

        private static void AddClassStatistics(ColumnDescriptor column, int term, string academicYear)
        {
            column.Item().PaddingTop(15).Text("CLASS STATISTICS").FontSize(12).Bold();
            column.Item().PaddingTop(5).Column(col =>
            {
                col.Item().Text($"Class Size: 45 | Position in Class: {new Random().Next(1, 46)}");
                col.Item().Text($"Term: {term} | Academic Year: {academicYear}");
            });
        }
        
         private static void AddPreschoolAdministrativeSection(ColumnDescriptor column, Grade grade)
        {
            column.Item().PaddingTop(15).Text("HEADTEACHER'S APPROVAL").AlignCenter().FontSize(14).Bold();
            
            column.Item().PaddingHorizontal(100).PaddingVertical(60).Row(row =>
            {
                 row.RelativeItem().Background(Colors.Grey.Lighten3).Padding(10).Height(60).Column(col =>
                {
                    col.Item().Text("Signature:").AlignCenter().Bold();
                    col.Item().PaddingVertical(5).AlignCenter().Height(35).Image("./Media/pre-sig.png");
                    col.Item().LineHorizontal(1);
                });
            });
        }

        private static void AddPrimaryAdministrativeSection(ColumnDescriptor column, Grade grade)
        {
            column.Item().PaddingTop(15).Text("HEADTEACHER'S APPROVAL").AlignCenter().FontSize(14).Bold();

         
            column.Item().PaddingHorizontal(100).PaddingVertical(60).Row(row =>
            {
                row.RelativeItem().Background(Colors.Grey.Lighten3).Padding(10).Height(60).Column(col =>
               {
                   col.Item().Text("Signature:").AlignCenter().Bold();
                   col.Item().PaddingVertical(5).AlignCenter().Height(35).Image("./Media/pri-sig.png");
                   col.Item().LineHorizontal(1);
               });
            });
        }

        private static void AddSecondaryAdministrativeSection(ColumnDescriptor column, Grade grade)
        {
            column.Item().PaddingTop(15).Text("HEADTEACHER'S APPROVAL").AlignCenter().FontSize(14).Bold();
            
            column.Item().PaddingHorizontal(100).PaddingVertical(60).Row(row =>
            {
                
                
                row.RelativeItem().Background(Colors.Grey.Lighten3).Padding(10).Height(60).Column(col =>
                {
                    col.Item().Text("Signature:").AlignCenter().Bold();
                    col.Item().PaddingVertical(5).AlignCenter().Height(35).Image("./Media/sec-sig.png");
                    col.Item().LineHorizontal(1);
                });
            });
        }

        private static void AddPreschoolGradingScale(ColumnDescriptor column)
        {
            column.Item().PaddingTop(30).Text("PRESCHOOL ASSESSMENT SCALE").FontSize(12).Bold();
            column.Item().PaddingTop(10).Column(col =>
            {
                col.Item().Text("★★★★★ - Exceeds Expectations");
                col.Item().Text("★★★★☆ - Meets Expectations");
                col.Item().Text("★★★☆☆ - Approaching Expectations");
                col.Item().Text("★★☆☆☆ - Needs Support");
            });
        }

        private static void AddPrimaryGradingScale(ColumnDescriptor column)
        {
            column.Item().PaddingTop(25).Text("PRIMARY SCHOOL GRADING SCALE").FontSize(12).AlignCenter().Bold();

            column.Item().PaddingTop(10).PaddingHorizontal(80).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                   {
                       columns.ConstantColumn(40);
                       columns.RelativeColumn(2);
                       columns.RelativeColumn(3);
                   });

                table.Cell().ColumnSpan(3)
               .Background(Colors.Grey.Lighten2).Element(CellStyle)
               .Text("Grading Scale").AlignCenter();

                table.Cell().Element(CellStyle).Text("Grade").SemiBold();
                table.Cell().Element(CellStyle).Text("Range").SemiBold();
                table.Cell().Element(CellStyle).Text("Remark").SemiBold();

                table.Cell().Element(CellStyle).Text("A");
                table.Cell().Element(CellStyle).Text("(80-100)");
                table.Cell().Element(CellStyle).Text("Excellent");

                table.Cell().Element(CellStyle).Text("B");
                table.Cell().Element(CellStyle).Text("(70-79)");
                table.Cell().Element(CellStyle).Text("Very Good");

                table.Cell().Element(CellStyle).Text("C");
                table.Cell().Element(CellStyle).Text("(60-69)");
                table.Cell().Element(CellStyle).Text("Good");

                table.Cell().Element(CellStyle).Text("D");
                table.Cell().Element(CellStyle).Text("(50-59)");
                table.Cell().Element(CellStyle).Text("Satisfactory");
                
                table.Cell().Element(CellStyle).Text("F");
                table.Cell().Element(CellStyle).Text("(0-49)");
                table.Cell().Element(CellStyle).Text("Needs Improvement");
            });

            // column.Item().PaddingTop(10).Column(col =>
            // {
            //     col.Item().Text("A (80-100) - Excellent | B (70-79) - Very Good");
            //     col.Item().Text("C (60-69) - Good | D (50-59) - Satisfactory | F (0-49) - Needs Improvement");
            // });
        }

        private static void AddSecondaryGradingScale(ColumnDescriptor column)
        {
            column.Item().PaddingTop(25).Text("SECONDARY SCHOOL GRADING SCALE").FontSize(12).AlignCenter().Bold();

            column.Item().PaddingTop(10).PaddingHorizontal(80).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                   {
                       columns.ConstantColumn(40);
                       columns.RelativeColumn(2);
                       columns.RelativeColumn(3);
                   });

                table.Cell().ColumnSpan(3)
               .Background(Colors.Grey.Lighten2).Element(CellStyle)
               .Text("Grading Scale").AlignCenter();

                table.Cell().Element(CellStyle).Text("Grade").SemiBold();
                table.Cell().Element(CellStyle).Text("Range").SemiBold();
                table.Cell().Element(CellStyle).Text("Remark").SemiBold();

                table.Cell().Element(CellStyle).Text("A");
                table.Cell().Element(CellStyle).Text("(80-100)");
                table.Cell().Element(CellStyle).Text("Excellent");

                table.Cell().Element(CellStyle).Text("B");
                table.Cell().Element(CellStyle).Text("(70-79)");
                table.Cell().Element(CellStyle).Text("Very Good");

                table.Cell().Element(CellStyle).Text("C");
                table.Cell().Element(CellStyle).Text("(60-69)");
                table.Cell().Element(CellStyle).Text("Good");

                table.Cell().Element(CellStyle).Text("D");
                table.Cell().Element(CellStyle).Text("(50-59)");
                table.Cell().Element(CellStyle).Text("Satisfactory");
                
                table.Cell().Element(CellStyle).Text("F");
                table.Cell().Element(CellStyle).Text("(0-49)");
                table.Cell().Element(CellStyle).Text("Needs Improvement");
            });
        }

        private static void AddContactInfo(ColumnDescriptor column)
        {
            column.Item().Background(Colors.Grey.Lighten4).PaddingTop(10).Column(contact =>
                    {
                        contact.Item().Text("Chudleigh House School").FontSize(14).Bold().AlignCenter();
                        contact.Item().Text("Plot 11289, Lusaka, Zambia").FontSize(8).AlignCenter();
                        contact.Item().Text("Tel: +260-955-876333  | +260-953-074465").FontSize(12).AlignCenter().FontSize(8);
                        contact.Item().Text("Email: info@chudleighhouseschool.com").AlignCenter().FontSize(8);
                        contact.Item().Text("Website: www.chudleighhouseschool.com").FontSize(8).AlignCenter();
                    });

        }

        private static void AddCoverPage(IDocumentContainer container, Student student, AcademicYear academicYear, int term, string section)
        {
            
            container.Page(page =>
            {
                 page.Background()
                .AlignCenter()
                .AlignMiddle()
                .Width(500)
                .Image("./Media/chs-wm-logo.png");

                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));
                page.Content().Border(5).BorderColor(Colors.Blue.Darken2).Padding(20).Column(column =>
                {
                    
                    // School Name
                    column.Item().PaddingTop(30).AlignCenter()
                        .Text("CHUDLEIGH HOUSE SCHOOL").FontSize(32).Bold().FontColor(Colors.Blue.Darken2);
                    // Logo
                    column.Item().AlignCenter().Height(120).Image("./Media/chs-logo.png");

                    // School Motto
                    column.Item().PaddingTop(15).AlignCenter()
                        .Text("\"Towards A Brighter Future\"").FontSize(20).Italic().FontColor(Colors.Blue.Medium);

                    // Report Title
                    column.Item().PaddingTop(80).AlignCenter()
                        .Text($"{section} REPORT CARD").FontSize(24).Bold().FontColor(Colors.Green.Darken1);

                    // Exam Title
                    column.Item().PaddingTop(20).AlignCenter()
                        .Text($"End of Term {term} Examination {academicYear.Name}").FontSize(18).Bold().FontColor(Colors.Blue.Darken1);

                    column.Item().PaddingTop(100).Border(0).BorderColor(Colors.Transparent).PaddingHorizontal(60).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                       {
                           columns.ConstantColumn(80);
                           columns.RelativeColumn(125);
                           
                       });


                        table.Cell().Element(CellStyle).Text($"Name:").FontSize(14);
                        table.Cell().Element(CellStyle).Text($"{student.FullName}").FontSize(14);

                        table.Cell().Element(CellStyle).Text($"Class:").FontSize(14);
                        table.Cell().Element(CellStyle).Text($"{student.Grade?.FullName}").FontSize(14);
                        
                        table.Cell().Element(CellStyle).Text($"Teacher:").FontSize(14);
                        table.Cell().Element(CellStyle).Text($"{student.Grade?.HomeroomTeacher?.FullName ?? "Not Assigned"}").FontSize(14);
                        

                    });

                    // School Address and Contact
                     column.Item().PaddingTop(100).Column(contact =>
                     {
                         //contact.Item().Text("Chudleigh House School").FontSize(14).Bold().AlignCenter();
                         contact.Item().Text("Plot 11289, Lusaka, Zambia").FontSize(8).AlignCenter();
                         contact.Item().Text("Tel: +260-955-876333  | +260-953-074465").FontSize(12).AlignCenter().FontSize(8);
                         contact.Item().Text("Email: info@chudleighhouseschool.com").AlignCenter().FontSize(8);
                         contact.Item().Text("Website: www.chudleighhouseschool.com").FontSize(8).AlignCenter();
                     });

                    
                });

            });
        }

        private static void AddPageFooter(PageDescriptor page, int pageNumber)
        {
            page.Footer().AlignCenter().Text($"Page {pageNumber} of 4 | Generated on {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container.Border(1).BorderColor(Colors.Grey.Medium).Padding(5).AlignMiddle();
        }

        // Calculation Methods
        private static decimal CalculateOverallAverage(List<StudentExamData> examData)
        {
            if (!examData.Any()) return 0;
            
            var averages = examData.Select(e => (e.Test1Score + e.MidTermScore + e.EndTermScore) / 3);
            return averages.Average();
        }

        private static decimal CalculateAveragePoints(List<StudentExamData> examData)
        {
            if (!examData.Any()) return 0;
            
            var totalPoints = examData.Select(e =>
            {
                var total = e.Test1Score + e.MidTermScore + e.EndTermScore;
                var grade = GetSecondaryGrade(total);
                return GetGradePoints(grade);
            }).Sum();
            
            return totalPoints / examData.Count;
        }

        // Comment Generation Methods
        private static string GetEndOfTermComments(List<StudentExamData> examData, string firstName)
        {
            var commentsWithContent = examData.Where(e => !string.IsNullOrWhiteSpace(e.Comments)).ToList();
            
            if (commentsWithContent.Any())
            {
                var firstComment = commentsWithContent.First();
                return $"{firstName} shows progress in {firstComment.SubjectName.ToLower()}. {firstComment.Comments}";
            }
            
            return $"{firstName} has shown wonderful progress this term. Continue encouraging learning through play and exploration.";
        }

        private static string GetSkillComments(List<StudentExamData> examData, string skillArea)
        {
            // Map skill areas to subjects if available
            var relevantSubject = examData.FirstOrDefault(e => 
                e.SubjectName.ToLower().Contains(skillArea.Split(' ')[0].ToLower()));
            
            if (relevantSubject != null && !string.IsNullOrWhiteSpace(relevantSubject.Comments))
            {
                return relevantSubject.Comments.Length > 50 
                    ? relevantSubject.Comments.Substring(0, 47) + "..."
                    : relevantSubject.Comments;
            }
            
            return "Shows good development";
        }

        private static string GenerateProgressSummary(List<StudentExamData> examData, string firstName)
        {
            var overallAverage = CalculateOverallAverage(examData);
            var commentsCount = examData.Count(e => !string.IsNullOrWhiteSpace(e.Comments));
            
            return $"{firstName} continues to show remarkable growth in all developmental areas. " +
                   $"Based on {examData.Count} assessment areas with {commentsCount} detailed comments from teachers, " +
                   $"the overall performance shows {GetPerformanceLevel(overallAverage)} development. " +
                   $"Encouragement in creative activities would benefit further development.";
        }
        
            private const decimal EXCELLENT_THRESHOLD = 85m;
            private const decimal GOOD_THRESHOLD = 75m;
            private const decimal SATISFACTORY_THRESHOLD = 60m;

            private static string GenerateTeacherAssessment(List<StudentExamData> examData, string firstName)
            {
            var overallAverage = CalculateOverallAverage(examData);

            var excellentSubjects = examData.Where(e => CalculateSubjectAverage(e) >= EXCELLENT_THRESHOLD).ToList();
            var goodSubjects = examData.Where(e => CalculateSubjectAverage(e) >= GOOD_THRESHOLD && CalculateSubjectAverage(e) < EXCELLENT_THRESHOLD).ToList();
            var satisfactorySubjects = examData.Where(e => CalculateSubjectAverage(e) >= SATISFACTORY_THRESHOLD && CalculateSubjectAverage(e) < GOOD_THRESHOLD).ToList();
            var improvementSubjects = examData.Where(e => CalculateSubjectAverage(e) < SATISFACTORY_THRESHOLD).ToList();

            var assessment = $"Throughout this term, {firstName} has demonstrated {GetPerformanceLevel(overallAverage)} development " +
                           $"across all assessment areas. ";

            if (excellentSubjects.Any())
            {
                assessment += $"Excellent performance shown in {string.Join(", ", excellentSubjects.Select(s => s.SubjectName))}. ";
            }

            if (goodSubjects.Any())
            {
                assessment += $"Good progress demonstrated in {string.Join(", ", goodSubjects.Select(s => s.SubjectName))}. ";
            }

            if (satisfactorySubjects.Any())
            {
                assessment += $"Satisfactory development observed in {string.Join(", ", satisfactorySubjects.Select(s => s.SubjectName))}. ";
            }

            if (improvementSubjects.Any())
            {
                assessment += $"Areas requiring focused attention include {string.Join(", ", improvementSubjects.Select(s => s.SubjectName))}. ";
            }

            assessment += GetRecommendation(overallAverage, improvementSubjects.Count, examData.Count);

            return assessment;
        }

        private static decimal CalculateSubjectAverage(StudentExamData exam)
        {
            return (exam.Test1Score + exam.MidTermScore + exam.EndTermScore) / 3m;
        }

        private static string GetRecommendation(decimal overallAverage, int improvementSubjectsCount, int totalSubjects)
        {
            if (overallAverage >= EXCELLENT_THRESHOLD)
            {
                return "I recommend continued challenge and enrichment opportunities to maintain this excellent trajectory.";
            }
            else if (overallAverage >= GOOD_THRESHOLD)
            {
                return "I recommend sustained effort and targeted practice to build upon this solid foundation.";
            }
            else if (improvementSubjectsCount >= totalSubjects / 2)
            {
                return "I recommend additional support and structured practice across multiple areas for comprehensive improvement.";
            }
            else
            {
                return "I recommend continued encouragement and focused support in identified areas for optimal development.";
            }
        }


        // private static string GenerateTeacherAssessment(List<StudentExamData> examData, string firstName)
        // {
        //     var overallAverage = CalculateOverallAverage(examData);
        //     var strongSubjects = examData.Where(e => (e.Test1Score + e.MidTermScore + e.EndTermScore) / 3 >= 80).ToList();
        //     var improvementSubjects = examData.Where(e => (e.Test1Score + e.MidTermScore + e.EndTermScore) / 3 < 60).ToList();

        //     var assessment = $"Throughout this term, {firstName} has demonstrated {GetPerformanceLevel(overallAverage)} development " +
        //                    $"across all assessment areas. ";

        //     if (strongSubjects.Any())
        //     {
        //         assessment += $"Particular strength shown in {string.Join(", ", strongSubjects.Select(s => s.SubjectName))}. ";
        //     }

        //     if (improvementSubjects.Any())
        //     {
        //         assessment += $"Areas for continued focus include {string.Join(", ", improvementSubjects.Select(s => s.SubjectName))}. ";
        //     }

        //     assessment += $"I recommend continued encouragement and support for optimal development.";

        //     return assessment;
        // }

        private static string GenerateDetailedTeacherComment(decimal average, string firstName, List<StudentExamData> examData)
        {
            var subjectsWithComments = examData.Where(e => !string.IsNullOrWhiteSpace(e.Comments)).ToList();
            var strongSubjects = examData.Where(e => (e.Test1Score + e.MidTermScore + e.EndTermScore) / 3 >= 80).ToList();
            
            var comment = $"Throughout this term, {firstName} has {GetPerformanceDescription(average)} academic ability. ";
            
            if (subjectsWithComments.Any())
            {
                comment += $"Specific teacher feedback highlights: {subjectsWithComments.First().Comments.Substring(0, Math.Min(100, subjectsWithComments.First().Comments.Length))}";
                if (subjectsWithComments.First().Comments.Length > 100) comment += "...";
                comment += " ";
            }
            
            if (strongSubjects.Any())
            {
                comment += $"Excellent performance in {string.Join(", ", strongSubjects.Take(2).Select(s => s.SubjectName))}. ";
            }
            
            comment += GetAdviceBasedOnPerformance(average);
            
            return comment;
        }

        private static string GenerateSubjectRecommendations(List<StudentExamData> examData, string firstName)
        {
            var recommendations = new List<string>();
            
            foreach (var exam in examData.Take(3))
            {
                var average = (exam.Test1Score + exam.MidTermScore + exam.EndTermScore) / 3;
                var recommendation = "";
                
                if (average >= 80)
                    recommendation = $"{exam.SubjectName}: Excellent performance - consider advanced challenges.";
                else if (average >= 70)
                    recommendation = $"{exam.SubjectName}: Very good progress - focus on consistency.";
                else if (average >= 60)
                    recommendation = $"{exam.SubjectName}: Good effort - strengthen weak areas.";
                else
                    recommendation = $"{exam.SubjectName}: Needs improvement - seek additional support.";
                
                // Add specific comments if available
                if (!string.IsNullOrWhiteSpace(exam.Comments))
                {
                    recommendation += $" Teacher notes: {exam.Comments.Substring(0, Math.Min(50, exam.Comments.Length))}";
                    if (exam.Comments.Length > 50) recommendation += "...";
                }
                
                recommendations.Add(recommendation);
            }
            
            return string.Join(" ", recommendations);
        }

        private static string GenerateSecondaryTeacherComment(decimal averagePoints, string firstName, List<StudentExamData> examData)
        {
            var subjectsWithComments = examData.Where(e => !string.IsNullOrWhiteSpace(e.Comments)).ToList();
            var topSubjects = examData.OrderByDescending(e => e.Test1Score + e.MidTermScore + e.EndTermScore).Take(2).ToList();
            
            var comment = $"{firstName} has {GetSecondaryPerformanceDescription(averagePoints)} this term ";
            
            if (topSubjects.Any())
            {
                comment += $"with outstanding results in {string.Join(" and ", topSubjects.Select(s => s.SubjectName))}. ";
            }
            
            if (subjectsWithComments.Any())
            {
                var firstComment = subjectsWithComments.First();
                comment += $"Teacher feedback in {firstComment.SubjectName}: {firstComment.Comments.Substring(0, Math.Min(80, firstComment.Comments.Length))}";
                if (firstComment.Comments.Length > 80) comment += "...";
                comment += " ";
            }
            
            comment += GetSecondaryAdvice(averagePoints);
            
            return comment;
        }

        private static string GenerateSecondaryPerformanceAnalysis(decimal averagePoints, string firstName)
        {
            if (averagePoints >= 10)
                return $"{firstName} demonstrates outstanding academic excellence with consistent high performance across all subjects. " +
                       $"This level of achievement reflects strong analytical skills, excellent study habits, and deep understanding of complex concepts. " +
                       $"Based on End-of-Term teacher feedback, continue to pursue academic challenges and consider leadership roles.";
            if (averagePoints >= 8)
                return $"{firstName} shows very commendable academic performance with strong understanding in most subject areas. " +
                       $"Teacher comments indicate clear potential for achieving excellence with continued focus and effort. " +
                       $"Consider developing stronger study strategies in subjects with lower performance.";
            if (averagePoints >= 6)
                return $"{firstName} demonstrates good academic progress overall but shows inconsistency across different subjects. " +
                       $"End-of-Term feedback suggests focusing on developing more effective study techniques and time management skills.";
            if (averagePoints >= 4)
                return $"{firstName} shows satisfactory performance but needs significant improvement to meet full academic potential. " +
                       $"Teacher comments recommend developing a structured study schedule and seeking additional support.";
            return $"{firstName} requires immediate and intensive academic support across multiple subjects. " +
                   $"Based on teacher feedback, consider academic counseling and comprehensive support planning.";
        }

        private static string GenerateHeadTeacherComment(decimal averagePoints)
        {
            if (averagePoints >= 10)
                return "Outstanding academic achievement demonstrated across all subjects. This student shows exceptional ability, " +
                       "strong work ethic, and excellent potential for future academic success. Continue pursuing excellence.";
            if (averagePoints >= 8)
                return "Very commendable academic performance with strong potential evident. The student demonstrates good " +
                       "understanding and consistent effort. Continue working toward excellence through focused study.";
            if (averagePoints >= 6)
                return "Good overall academic effort shown with room for improvement in specific areas. The student should " +
                       "focus on developing stronger study strategies and seek support where needed for optimal performance.";
            if (averagePoints >= 4)
                return "Satisfactory performance overall, but more dedication and consistent effort are required. The student " +
                       "needs to develop better study habits and utilize available academic resources more effectively.";
            return "Immediate improvement required across all academic areas. Comprehensive support, close monitoring, " +
                   "and intervention strategies are essential to help the student meet educational standards.";
        }

        // Utility Methods
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
            if (totalScore >= 240) return "A";
            if (totalScore >= 210) return "B";
            if (totalScore >= 180) return "C";
            if (totalScore >= 150) return "D";
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

        private static string GetPerformanceLevel(decimal average)
        {
            if (average >= 80) return "excellent";
            if (average >= 70) return "very good";
            if (average >= 60) return "good";
            if (average >= 50) return "satisfactory";
            return "developing";
        }

        private static string GetPerformanceDescription(decimal average)
        {
            if (average >= 80) return "consistently demonstrated exceptional";
            if (average >= 70) return "shown very good";
            if (average >= 60) return "displayed good";
            if (average >= 50) return "shown satisfactory";
            return "needs to develop stronger";
        }

        private static string GetAdviceBasedOnPerformance(decimal average)
        {
            if (average >= 80) return "Continue with the same dedication and excellence.";
            if (average >= 70) return "With continued effort, even better results can be achieved.";
            if (average >= 60) return "Focus on areas that need improvement for better results.";
            if (average >= 50) return "More effort and dedication are needed to improve grades.";
            return "Significant improvement required. Seek additional support and study time.";
        }

        private static string GetSecondaryPerformanceDescription(decimal averagePoints)
        {
            if (averagePoints >= 10) return "excelled academically";
            if (averagePoints >= 8) return "performed very well";
            if (averagePoints >= 6) return "shown good progress";
            if (averagePoints >= 4) return "demonstrated satisfactory performance";
            return "requires significant academic improvement";
        }

        private static string GetSecondaryAdvice(decimal averagePoints)
        {
            if (averagePoints >= 10) return "Maintain this excellent standard and continue challenging yourself.";
            if (averagePoints >= 8) return "Continue working hard to achieve excellence in all subjects.";
            if (averagePoints >= 6) return "Focus on weaker subjects for more balanced improvement.";
            if (averagePoints >= 4) return "More effort and dedicated study time are needed for improvement.";
            return "Immediate attention and comprehensive academic support are required.";
        }
    }

    // Data Transfer Object for Student Exam Data
    public class StudentExamData
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; }
        public decimal Test1Score { get; set; }
        public decimal MidTermScore { get; set; }
        public decimal EndTermScore { get; set; }
        public string Comments { get; set; } // Only from End-of-Term exam
        public DateTime? CommentsUpdatedAt { get; set; }
        public string CommentsUpdatedBy { get; set; }
        public DateTime LastUpdated { get; set; }
        public string RecordedBy { get; set; }
    }
}