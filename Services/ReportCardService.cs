using BluebirdCore.Data;
using BluebirdCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace BluebirdCore.Services
{
    public interface IReportCardService
    {
        Task<ReportCard> GenerateReportCardAsync(int studentId, int academicYear, int term, int generatedBy);
        Task<IEnumerable<ReportCard>> GenerateClassReportCardsAsync(int gradeId, int academicYear, int term, int generatedBy);
        Task<byte[]> GetReportCardPdfAsync(int reportCardId);
        Task<IEnumerable<ReportCard>> GetStudentReportCardsAsync(int studentId);
    }

    public class ReportCardService : IReportCardService
    {
        private readonly SchoolDbContext _context;
        private readonly IReportCardPdfService _pdfService;

        public ReportCardService(SchoolDbContext context, IReportCardPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        public async Task<ReportCard> GenerateReportCardAsync(int studentId, int academicYear, int term, int generatedBy)
        {
            var student = await _context.Students
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null) throw new ArgumentException("Student not found");

            var scores = await _context.ExamScores
                .Include(es => es.Subject)
                .Include(es => es.ExamType)
                .Where(es => es.StudentId == studentId && es.AcademicYear == academicYear && es.Term == term)
                .ToListAsync();

            var filePath = await _pdfService.GenerateReportCardPdfAsync(student, scores, academicYear, term);

            var reportCard = new ReportCard
            {
                StudentId = studentId,
                GradeId = student.GradeId,
                AcademicYear = academicYear,
                Term = term,
                FilePath = filePath,
                GeneratedBy = generatedBy
            };

            _context.ReportCards.Add(reportCard);
            await _context.SaveChangesAsync();

            return reportCard;
        }

        public async Task<IEnumerable<ReportCard>> GenerateClassReportCardsAsync(int gradeId, int academicYear, int term, int generatedBy)
        {
            var students = await _context.Students
                .Where(s => s.GradeId == gradeId && !s.IsArchived)
                .ToListAsync();

            var reportCards = new List<ReportCard>();

            foreach (var student in students)
            {
                var reportCard = await GenerateReportCardAsync(student.Id, academicYear, term, generatedBy);
                reportCards.Add(reportCard);
            }

            return reportCards;
        }

        public async Task<byte[]> GetReportCardPdfAsync(int reportCardId)
        {
            var reportCard = await _context.ReportCards.FindAsync(reportCardId);
            if (reportCard == null || !File.Exists(reportCard.FilePath))
                return null;

            return await File.ReadAllBytesAsync(reportCard.FilePath);
        }

        public async Task<IEnumerable<ReportCard>> GetStudentReportCardsAsync(int studentId)
        {
            return await _context.ReportCards
                .Where(rc => rc.StudentId == studentId)
                .OrderByDescending(rc => rc.AcademicYear)
                .ThenByDescending(rc => rc.Term)
                .ToListAsync();
        }
    }
}