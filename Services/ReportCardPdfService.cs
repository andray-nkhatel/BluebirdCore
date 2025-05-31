using BluebirdCore.Entities;

namespace BluebirdCore.Services
{
     public interface IReportCardPdfService
    {
        Task<string> GenerateReportCardPdfAsync(Student student, IEnumerable<ExamScore> scores, int academicYear, int term);
    }

    public class ReportCardPdfService : IReportCardPdfService
    {
        private readonly string _reportPath;

        public ReportCardPdfService(IConfiguration configuration)
        {
            _reportPath = configuration["ReportCards:StoragePath"] ?? "Reports";
            Directory.CreateDirectory(_reportPath);
        }

        public async Task<string> GenerateReportCardPdfAsync(Student student, IEnumerable<ExamScore> scores, int academicYear, int term)
        {
            var fileName = $"ReportCard_{student.StudentNumber}_{academicYear}_Term{term}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            var filePath = Path.Combine(_reportPath, fileName);

            // Generate PDF based on school section
            switch (student.Grade.Section)
            {
                case SchoolSection.Preschool:
                    await GeneratePreschoolReportCardAsync(student, scores, academicYear, term, filePath);
                    break;
                case SchoolSection.Primary:
                    await GeneratePrimaryReportCardAsync(student, scores, academicYear, term, filePath);
                    break;
                case SchoolSection.Secondary:
                    await GenerateSecondaryReportCardAsync(student, scores, academicYear, term, filePath);
                    break;
            }

            return filePath;
        }

        private async Task GeneratePreschoolReportCardAsync(Student student, IEnumerable<ExamScore> scores, int academicYear, int term, string filePath)
        {
            // Implement QuestPDF generation for Preschool format
            await Task.Run(() =>
            {
                // Placeholder for QuestPDF implementation
                // This would use QuestPDF to create a preschool-specific report card
                File.WriteAllText(filePath + ".txt", $"Preschool Report Card for {student.FullName}");
            });
        }

        private async Task GeneratePrimaryReportCardAsync(Student student, IEnumerable<ExamScore> scores, int academicYear, int term, string filePath)
        {
            // Implement QuestPDF generation for Primary format
            await Task.Run(() =>
            {
                // Placeholder for QuestPDF implementation
                File.WriteAllText(filePath + ".txt", $"Primary Report Card for {student.FullName}");
            });
        }

        private async Task GenerateSecondaryReportCardAsync(Student student, IEnumerable<ExamScore> scores, int academicYear, int term, string filePath)
        {
            // Implement QuestPDF generation for Secondary format
            await Task.Run(() =>
            {
                // Placeholder for QuestPDF implementation
                File.WriteAllText(filePath + ".txt", $"Secondary Report Card for {student.FullName}");
            });
        }
    }

}