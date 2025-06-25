using BluebirdCore.Data;
using BluebirdCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
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

        // Semaphore for concurrent operations - increased since we're not storing PDFs
        private readonly SemaphoreSlim _concurrencySemaphore;

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
            
            // Can increase concurrency since we're not storing PDFs in memory
            _concurrencySemaphore = new SemaphoreSlim(Environment.ProcessorCount * 2, Environment.ProcessorCount * 2);
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
                    _logger.LogInformation("Report card already exists for Student {StudentId}, Year {Year}, Term {Term}",
                        studentId, academicYear, term);
                    return existingReportCard;
                }

                // Validate that student and user exist
                var studentExists = await _context.Students.AnyAsync(s => s.Id == studentId);
                if (!studentExists)
                {
                    throw new ArgumentException($"Student with ID {studentId} not found");
                }

                var userExists = await _context.Users.AnyAsync(u => u.Id == generatedBy);
                if (!userExists)
                {
                    throw new ArgumentException($"User with ID {generatedBy} not found");
                }

                // Get student's grade for the report card record
                var student = await _context.Students
                    .Select(s => new { s.Id, s.GradeId })
                    .FirstAsync(s => s.Id == studentId);

                // Create report card entity WITHOUT PDF content
                var reportCard = new ReportCard
                {
                    StudentId = studentId,
                    GradeId = student.GradeId,
                    AcademicYear = academicYear,
                    Term = term,
                    // PdfContent = null, // Don't store PDF content
                    GeneratedBy = generatedBy
                };

                _context.ReportCards.Add(reportCard);
                await _context.SaveChangesAsync();

                // Return the report card with navigation properties loaded
                var savedReportCard = await _context.ReportCards
                    .Include(rc => rc.Student)
                    .Include(rc => rc.Grade)
                    .Include(rc => rc.GeneratedByUser)
                    .FirstAsync(rc => rc.Id == reportCard.Id);

                _logger.LogInformation("Report card record created successfully for Student {StudentId}, Year {Year}, Term {Term}",
                    studentId, academicYear, term);

                return savedReportCard;
            }
            catch (Exception ex) when (!(ex is ArgumentException))
            {
                _logger.LogError(ex, "Unexpected error creating report card record for Student {StudentId}", studentId);
                throw new InvalidOperationException("An error occurred while creating the report card record", ex);
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

                // Validate user exists once
                var userExists = await _context.Users.AnyAsync(u => u.Id == generatedBy);
                if (!userExists)
                {
                    throw new ArgumentException($"User with ID {generatedBy} not found");
                }

                var reportCards = new ConcurrentBag<ReportCard>();
                var failedGenerations = new ConcurrentBag<(int StudentId, string ErrorMessage, Exception Error)>();

                // Can increase batch size since we're not generating PDFs in memory
                const int batchSize = 15;
                const int maxRetries = 2;

                for (int i = 0; i < students.Count; i += batchSize)
                {
                    var batch = students.Skip(i).Take(batchSize);
                    var batchTasks = batch.Select(async student =>
                    {
                        await _concurrencySemaphore.WaitAsync();
                        try
                        {
                            for (int attempt = 1; attempt <= maxRetries; attempt++)
                            {
                                try
                                {
                                    if (attempt > 1)
                                    {
                                        await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(50, 200)));
                                    }

                                    using var scope = _serviceProvider.CreateScope();
                                    var scopedContext = scope.ServiceProvider.GetRequiredService<SchoolDbContext>();

                                    var reportCard = await CreateReportCardRecordAsync(
                                        scopedContext, student.Id, student.GradeId, academicYear, term, generatedBy);
                                    
                                    if (reportCard != null)
                                    {
                                        reportCards.Add(reportCard);
                                        _logger.LogDebug("Successfully created report card record for Student {StudentId} on attempt {Attempt}", 
                                            student.Id, attempt);
                                        return;
                                    }
                                }
                                catch (Exception ex) when (attempt < maxRetries)
                                {
                                    _logger.LogWarning(ex, "Attempt {Attempt} failed for Student {StudentId}, retrying...", 
                                        attempt, student.Id);
                                    continue;
                                }
                                catch (Exception ex)
                                {
                                    var errorMessage = GetDetailedErrorMessage(ex);
                                    _logger.LogError(ex, "All attempts failed for Student {StudentId}. Error: {ErrorMessage}", 
                                        student.Id, errorMessage);
                                    failedGenerations.Add((student.Id, errorMessage, ex));
                                    return;
                                }
                            }
                        }
                        finally
                        {
                            _concurrencySemaphore.Release();
                        }
                    });

                    await Task.WhenAll(batchTasks);

                    // Log progress for large batches
                    if (students.Count > 20)
                    {
                        var processed = Math.Min(i + batchSize, students.Count);
                        _logger.LogInformation("Processed {Processed}/{Total} students in Grade {GradeId}", 
                            processed, students.Count, gradeId);
                    }
                }

                var successCount = reportCards.Count;
                var failureCount = failedGenerations.Count;

                _logger.LogInformation("Created {SuccessCount} report card records for Grade {GradeId}. {FailedCount} failed.",
                    successCount, gradeId, failureCount);

                // Log detailed failure information
                if (failedGenerations.Any())
                {
                    var failureGroups = failedGenerations
                        .GroupBy(f => f.ErrorMessage)
                        .Select(g => new { ErrorMessage = g.Key, Count = g.Count(), StudentIds = g.Select(x => x.StudentId).ToArray() })
                        .ToList();

                    foreach (var group in failureGroups)
                    {
                        var studentIdsList = string.Join(", ", group.StudentIds.Take(10));
                        var moreStudents = group.StudentIds.Length > 10 ? $" and {group.StudentIds.Length - 10} more" : "";
                        
                        _logger.LogError("Error '{ErrorMessage}' occurred for {Count} students: {StudentIds}{MoreStudents}",
                            group.ErrorMessage, group.Count, studentIdsList, moreStudents);
                    }
                }

                return reportCards.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating class report card records for Grade {GradeId}", gradeId);
                throw new InvalidOperationException("Failed to create class report card records", ex);
            }
        }

        private async Task<ReportCard> CreateReportCardRecordAsync(
            SchoolDbContext context, int studentId, int gradeId, int academicYear, int term, int generatedBy)
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

            // Verify student still exists (could have been archived)
            var studentExists = await context.Students
                .AnyAsync(s => s.Id == studentId && !s.IsArchived);

            if (!studentExists)
            {
                throw new ArgumentException($"Student with ID {studentId} not found or is archived");
            }

            var reportCard = new ReportCard
            {
                StudentId = studentId,
                GradeId = gradeId,
                AcademicYear = academicYear,
                Term = term,
                // PdfContent = null, // Don't store PDF - generate on demand
                GeneratedBy = generatedBy
            };

            context.ReportCards.Add(reportCard);
            await context.SaveChangesAsync();

            return reportCard;
        }

        // This is where the magic happens - generate PDF on demand
        public async Task<byte[]> GetReportCardPdfAsync(int reportCardId)
        {
            try
            {
                if (reportCardId <= 0)
                {
                    throw new ArgumentException("Invalid report card ID", nameof(reportCardId));
                }

                // Check cache first - cache the PDF for a short time to avoid regenerating immediately
                string cacheKey = $"report_card_pdf_{reportCardId}";
                if (_cache.TryGetValue(cacheKey, out byte[] cachedPdf))
                {
                    _logger.LogDebug("Returning cached PDF for Report Card {ReportCardId}", reportCardId);
                    return cachedPdf;
                }

                // Get report card details
                var reportCard = await _context.ReportCards
                    .Include(rc => rc.Student)
                        .ThenInclude(s => s.Grade)
                    .FirstOrDefaultAsync(rc => rc.Id == reportCardId);

                if (reportCard == null)
                {
                    _logger.LogWarning("Report card with ID {ReportCardId} not found", reportCardId);
                    return null;
                }

                // Get student scores for the specific term/year
                var scores = await _context.ExamScores
                    .Include(es => es.Subject)
                    .Include(es => es.ExamType)
                    .Where(es => es.StudentId == reportCard.StudentId &&
                               es.AcademicYear == reportCard.AcademicYear &&
                               es.Term == reportCard.Term)
                    .ToListAsync();

                // Generate PDF on demand
                byte[] pdfBytes;
                try
                {
                    pdfBytes = await _pdfService.GenerateReportCardPdfAsync(
                        reportCard.Student, scores, reportCard.AcademicYear, reportCard.Term);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate PDF for Report Card {ReportCardId}", reportCardId);
                    throw new InvalidOperationException("Failed to generate report card PDF", ex);
                }

                // Cache for 10 minutes to avoid immediate regeneration
                _cache.Set(cacheKey, pdfBytes, TimeSpan.FromMinutes(10));

                _logger.LogInformation("Generated PDF on demand for Report Card {ReportCardId}", reportCardId);
                return pdfBytes;
            }
            catch (Exception ex) when (!(ex is ArgumentException || ex is InvalidOperationException))
            {
                _logger.LogError(ex, "Error generating PDF for Report Card {ReportCardId}", reportCardId);
                throw new InvalidOperationException("Failed to generate report card PDF", ex);
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

        private string GetDetailedErrorMessage(Exception ex)
        {
            return ex switch
            {
                ArgumentException => "Invalid argument provided",
                DbUpdateException dbEx => $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}",
                InvalidOperationException => "Operation failed",
                TimeoutException => "Operation timed out",
                OutOfMemoryException => "Insufficient memory",
                _ => ex.GetType().Name
            };
        }

        #region Private Helper Methods

        private void ValidateReportCardParameters(int id, int academicYear, int term, int generatedBy)
        {
            if (id <= 0)
                throw new ArgumentException("ID must be greater than 0", nameof(id));

            if (academicYear < 2000 || academicYear > DateTime.Now.Year + 1)
                throw new ArgumentException("Invalid academic year", nameof(academicYear));

            if (term < 1 || term > 4)
                throw new ArgumentException("Term must be between 1 and 3", nameof(term));

            if (generatedBy <= 0)
                throw new ArgumentException("GeneratedBy must be greater than 0", nameof(generatedBy));
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _concurrencySemaphore?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class SecurityException : Exception
    {
        public SecurityException(string message) : base(message) { }
        public SecurityException(string message, Exception innerException) : base(message, innerException) { }
    }
}