using BluebirdCore.Data;
using BluebirdCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace BluebirdCore.Services
{
    public interface IReportCardService
    {
        Task<ReportCard> GenerateReportCardAsync(int studentId, int academicYear, int term, int generatedBy);
        Task<IEnumerable<ReportCard>> GenerateClassReportCardsAsync(int gradeId, int academicYear, int term, int generatedBy);
        Task<byte[]> GetReportCardPdfAsync(int reportCardId);
        Task<IEnumerable<ReportCard>> GetStudentReportCardsAsync(int studentId);
        Task DeleteAllReportCardsAsync();
    }

    public class ReportCardService : IReportCardService
    {
        private readonly SchoolDbContext _context;
        private readonly ReportCardPdfService _pdfService;
        private readonly ILogger<ReportCardService> _logger;
        private readonly IMemoryCache _cache;

         private readonly IServiceProvider _serviceProvider;

        public ReportCardService(
            SchoolDbContext context,
            ReportCardPdfService pdfService,
            ILogger<ReportCardService> logger,
            IServiceProvider serviceProvider,
            IMemoryCache cache)
        {
            _context = context;
            _pdfService = pdfService;
            _logger = logger;
            _cache = cache;
            _serviceProvider = serviceProvider;
        }

         public async Task DeleteAllReportCardsAsync()
        {
            var allReportCards = _context.ReportCards.ToList();
            _context.ReportCards.RemoveRange(allReportCards);
            await _context.SaveChangesAsync();
        }


        public async Task<ReportCard> GenerateReportCardAsync(int studentId, int academicYear, int term, int generatedBy)
        {
            try
            {
                // Input validation
                ValidateReportCardParameters(studentId, academicYear, term, generatedBy);

                // Check for existing report card to prevent duplicates
                var existingReportCard = await _context.ReportCards
                    .FirstOrDefaultAsync(rc => rc.StudentId == studentId &&
                                             rc.AcademicYear == academicYear &&
                                             rc.Term == term);

                if (existingReportCard != null)
                {
                    _logger.LogWarning("Report card already exists for Student {StudentId}, Year {Year}, Term {Term}",
                        studentId, academicYear, term);
                    return existingReportCard;
                }

                // Single query to get all required data
                var studentData = await GetStudentWithRelatedDataAsync(studentId);
                if (studentData.student == null)
                {
                    throw new ArgumentException($"Student with ID {studentId} not found");
                }

                var generatedByUser = await GetUserAsync(generatedBy);
                if (generatedByUser == null)
                {
                    throw new ArgumentException($"User with ID {generatedBy} not found");
                }

                var scores = await GetStudentScoresAsync(studentId, academicYear, term);

                // Generate PDF in memory
                byte[] pdfBytes;
                try
                {
                    pdfBytes = await _pdfService.GenerateReportCardPdfAsync(
                        studentData.student, scores, academicYear, term);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate PDF for Student {StudentId}", studentId);
                    throw new InvalidOperationException("Failed to generate report card PDF", ex);
                }

                // Create report card entity - ONLY set foreign keys, not navigation properties
                var reportCard = new ReportCard
                {
                    StudentId = studentId,
                    GradeId = studentData.student.GradeId,
                    AcademicYear = academicYear,
                    Term = term,
                    PdfContent = pdfBytes,
                    GeneratedBy = generatedBy

                };

                _context.ReportCards.Add(reportCard);
                await _context.SaveChangesAsync();

                // If you need to return the report card with navigation properties loaded,
                // reload it from the database after saving
                var savedReportCard = await _context.ReportCards
                    .Include(rc => rc.Student)
                    .Include(rc => rc.Grade)
                    .Include(rc => rc.GeneratedByUser)
                    .FirstAsync(rc => rc.Id == reportCard.Id);

                _logger.LogInformation("Report card generated successfully for Student {StudentId}, Year {Year}, Term {Term}",
                    studentId, academicYear, term);

                return savedReportCard;
            }
            catch (Exception ex) when (!(ex is ArgumentException || ex is InvalidOperationException))
            {
                _logger.LogError(ex, "Unexpected error generating report card for Student {StudentId}", studentId);
                throw new InvalidOperationException("An error occurred while generating the report card", ex);
            }
        }




        public async Task<IEnumerable<ReportCard>> GenerateClassReportCardsAsync(int gradeId, int academicYear, int term, int generatedBy)
    {
        try
        {
            ValidateReportCardParameters(gradeId, academicYear, term, generatedBy);

            var students = await _context.Students
                .Where(s => s.GradeId == gradeId && !s.IsArchived)
                .Select(s => new { s.Id, s.GradeId })
                .ToListAsync();

            if (!students.Any())
            {
                _logger.LogWarning("No active students found for Grade {GradeId}", gradeId);
                return Enumerable.Empty<ReportCard>();
            }

            var reportCards = new List<ReportCard>();
            var failedGenerations = new List<(int StudentId, Exception Error)>();

            // Process in batches to avoid memory issues
            const int batchSize = 10;
            for (int i = 0; i < students.Count; i += batchSize)
            {
                var batch = students.Skip(i).Take(batchSize);
                var batchTasks = batch.Select(async student =>
                {
                    try
                    {
                        // Create a new scope for each parallel operation
                        using var scope = _serviceProvider.CreateScope();
                        var scopedContext = scope.ServiceProvider.GetRequiredService<SchoolDbContext>();

                        return await GenerateReportCardWithSeparateContextAsync(
                            scopedContext, student.Id, academicYear, term, generatedBy);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to generate report card for Student {StudentId}", student.Id);
                        lock (failedGenerations)
                        {
                            failedGenerations.Add((student.Id, ex));
                        }
                        return null;
                    }
                });

                var batchResults = await Task.WhenAll(batchTasks);
                reportCards.AddRange(batchResults.Where(rc => rc != null));
            }

            _logger.LogInformation("Generated {SuccessCount} report cards for Grade {GradeId}. {FailedCount} failed.",
                reportCards.Count, gradeId, failedGenerations.Count);

            if (failedGenerations.Any())
            {
                var failedStudentIds = string.Join(", ", failedGenerations.Select(f => f.StudentId));
                _logger.LogWarning("Failed to generate report cards for students: {FailedStudents}", failedStudentIds);
            }

            return reportCards;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating class report cards for Grade {GradeId}", gradeId);
            throw new InvalidOperationException("Failed to generate class report cards", ex);
        }
    }




        private async Task<ReportCard> GenerateReportCardWithSeparateContextAsync(
    SchoolDbContext context, int studentId, int academicYear, int term, int generatedBy)
        {
            // Check for existing report card
            var existingReportCard = await context.ReportCards
                .FirstOrDefaultAsync(rc => rc.StudentId == studentId &&
                                         rc.AcademicYear == academicYear &&
                                         rc.Term == term);

            if (existingReportCard != null)
            {
                return existingReportCard;
            }

            var student = await context.Students
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
            {
                throw new ArgumentException($"Student with ID {studentId} not found");
            }

            var scores = await context.ExamScores
                .Include(es => es.Subject)
                .Include(es => es.ExamType)
                .Where(es => es.StudentId == studentId &&
                           es.AcademicYear == academicYear &&
                           es.Term == term)
                .ToListAsync();

            var pdfBytes = await _pdfService.GenerateReportCardPdfAsync(
                student, scores, academicYear, term);

            var reportCard = new ReportCard
            {
                StudentId = studentId,
                GradeId = student.GradeId,
                AcademicYear = academicYear,
                Term = term,
                PdfContent = pdfBytes,
                GeneratedBy = generatedBy
            };

            context.ReportCards.Add(reportCard);
            await context.SaveChangesAsync(); // Save immediately with separate context

            return reportCard;
        }


        public async Task<byte[]> GetReportCardPdfAsync(int reportCardId)
        {
            try
            {
                if (reportCardId <= 0)
                {
                    throw new ArgumentException("Invalid report card ID", nameof(reportCardId));
                }

                var reportCard = await _context.ReportCards.FindAsync(reportCardId);
                if (reportCard == null)
                {
                    _logger.LogWarning("Report card with ID {ReportCardId} not found", reportCardId);
                    return null;
                }

                if (reportCard.PdfContent == null || reportCard.PdfContent.Length == 0)
                {
                    _logger.LogWarning("PDF content not found for Report Card {ReportCardId}", reportCardId);
                    return null;
                }

                return reportCard.PdfContent;
            }
            catch (Exception ex) when (!(ex is ArgumentException))
            {
                _logger.LogError(ex, "Error retrieving PDF for Report Card {ReportCardId}", reportCardId);
                throw new InvalidOperationException("Failed to retrieve report card PDF", ex);
            }
        }

        public async Task<IEnumerable<ReportCard>> GetStudentReportCardsAsync(int studentId)
        {
            try
            {
                if (studentId <= 0)
                {
                    throw new ArgumentException("Invalid student ID", nameof(studentId));
                }

                // Check cache first
                string cacheKey = $"student_report_cards_{studentId}";
                if (_cache.TryGetValue(cacheKey, out IEnumerable<ReportCard> cachedReportCards))
                {
                    return cachedReportCards;
                }

                var reportCards = await _context.ReportCards
                    .Where(rc => rc.StudentId == studentId)
                    .Include(rc => rc.Grade)
                    .Include(rc => rc.GeneratedByUser)
                    .OrderByDescending(rc => rc.AcademicYear)
                    .ThenByDescending(rc => rc.Term)
                    .ToListAsync();

                // Cache for 30 minutes
                _cache.Set(cacheKey, reportCards, TimeSpan.FromMinutes(30));

                return reportCards;
            }
            catch (Exception ex) when (!(ex is ArgumentException))
            {
                _logger.LogError(ex, "Error retrieving report cards for Student {StudentId}", studentId);
                throw new InvalidOperationException("Failed to retrieve student report cards", ex);
            }
        }

        #region Private Helper Methods


private async Task<ReportCard> GenerateReportCardInternalAsync(int studentId, int academicYear, int term, int generatedBy)
{
    // Check for existing report card
    var existingReportCard = await _context.ReportCards
        .FirstOrDefaultAsync(rc => rc.StudentId == studentId &&
                                 rc.AcademicYear == academicYear &&
                                 rc.Term == term);

    if (existingReportCard != null)
    {
        return existingReportCard;
    }

    var studentData = await GetStudentWithRelatedDataAsync(studentId);
    if (studentData.student == null)
    {
        throw new ArgumentException($"Student with ID {studentId} not found");
    }

    var generatedByUser = await GetUserAsync(generatedBy);
    var scores = await GetStudentScoresAsync(studentId, academicYear, term);

    var pdfBytes = await _pdfService.GenerateReportCardPdfAsync(
        studentData.student, scores, academicYear, term);

    // Create report card entity - ONLY set foreign keys
    var reportCard = new ReportCard
    {
        StudentId = studentId,
        GradeId = studentData.student.GradeId,
        AcademicYear = academicYear,
        Term = term,
        PdfContent = pdfBytes,
        GeneratedBy = generatedBy
       
    };

    _context.ReportCards.Add(reportCard);
    return reportCard;
}


        private async Task<(Student student, Grade grade)> GetStudentWithRelatedDataAsync(int studentId)
        {
            var result = await _context.Students
                .Include(s => s.Grade)
                .Where(s => s.Id == studentId)
                .Select(s => new { Student = s, Grade = s.Grade })
                .FirstOrDefaultAsync();

            return result != null ? (result.Student, result.Grade) : (null, null);
        }

        private async Task<User> GetUserAsync(int userId)
        {
            string cacheKey = $"user_{userId}";
            if (_cache.TryGetValue(cacheKey, out User cachedUser))
            {
                return cachedUser;
            }

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                _cache.Set(cacheKey, user, TimeSpan.FromHours(1));
            }

            return user;
        }

        private async Task<IEnumerable<ExamScore>> GetStudentScoresAsync(int studentId, int academicYear, int term)
        {
            return await _context.ExamScores
                .Include(es => es.Subject)
                .Include(es => es.ExamType)
                .Where(es => es.StudentId == studentId &&
                           es.AcademicYear == academicYear &&
                           es.Term == term)
                .ToListAsync();
        }

        private void ValidateReportCardParameters(int id, int academicYear, int term, int generatedBy)
        {
            if (id <= 0)
                throw new ArgumentException("ID must be greater than 0", nameof(id));

            if (academicYear < 2000 || academicYear > DateTime.Now.Year + 1)
                throw new ArgumentException("Invalid academic year", nameof(academicYear));

            if (term < 1 || term > 4)
                throw new ArgumentException("Term must be between 1 and 4", nameof(term));

            if (generatedBy <= 0)
                throw new ArgumentException("GeneratedBy must be greater than 0", nameof(generatedBy));
        }

        #endregion
    }

    public class SecurityException : Exception
    {
        public SecurityException(string message) : base(message) { }
        public SecurityException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    
}