using BluebirdCore.Data;
using BluebirdCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace BluebirdCore.Services
{
    public interface ITransitionPromotionService
    {
        Task<PromotionResult> PromoteStudentAsync(int studentId, int currentAcademicYear);
        Task<PromotionSummaryResult> PromoteAllStudentsAsync(int academicYearId, int currentAcademicYear);
        Task<List<Grade>> GetAvailablePromotionTargetsAsync(int fromGradeId, int currentAcademicYear);
        Task<TransitionStatusResult> GetTransitionStatusAsync(int academicYear);
    }

    public class TransitionPromotionService : ITransitionPromotionService
    {
        private readonly SchoolDbContext _context;

        public TransitionPromotionService(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<PromotionResult> PromoteStudentAsync(int studentId, int currentAcademicYear)
        {
            var result = new PromotionResult();

            try
            {
                var student = await _context.Students
                    .Include(s => s.Grade)
                    .FirstOrDefaultAsync(s => s.Id == studentId && !s.IsArchived);

                if (student == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Student not found or archived.";
                    return result;
                }

                var targetGrade = await FindNextGradeAsync(student.Grade, currentAcademicYear);

                if (targetGrade == null)
                {
                    result.Success = false;
                    result.ErrorMessage = GetPromotionErrorMessage(student.Grade, currentAcademicYear);
                    return result;
                }

                // Update student's grade
                student.GradeId = targetGrade.Id;
                await _context.SaveChangesAsync();

                result.Success = true;
                result.StudentsPromoted = 1;
                result.FromGrade = student.Grade.FullName;
                result.ToGrade = targetGrade.FullName;
                result.Message = $"Successfully promoted student from {result.FromGrade} to {result.ToGrade}.";

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Error promoting student: {ex.Message}";
                return result;
            }
        }

        public async Task<PromotionSummaryResult> PromoteAllStudentsAsync(int academicYearId, int currentAcademicYear)
        {
            var summary = new PromotionSummaryResult();

            try
            {
                // Get all active grades that have students and are valid for the current year
                var gradesWithStudents = await _context.Grades
                    .Include(g => g.Students.Where(s => !s.IsArchived))
                    .Where(g => g.IsActive && 
                               g.Students.Any(s => !s.IsArchived) &&
                               g.IsValidForYear(currentAcademicYear))
                    .OrderBy(g => g.Level)
                    .ThenBy(g => g.Stream)
                    .ToListAsync();

                summary.TotalGradesProcessed = gradesWithStudents.Count;

                foreach (var grade in gradesWithStudents)
                {
                    try
                    {
                        var targetGrade = await FindNextGradeAsync(grade, currentAcademicYear);
                        
                        if (targetGrade != null)
                        {
                            var studentsToPromote = grade.Students.Where(s => !s.IsArchived).ToList();
                            
                            foreach (var student in studentsToPromote)
                            {
                                student.GradeId = targetGrade.Id;
                            }

                            summary.SuccessfulPromotions++;
                            summary.TotalStudentsPromoted += studentsToPromote.Count;

                            summary.PromotionDetails.Add(new GradePromotionDetail
                            {
                                FromGrade = grade.FullName,
                                ToGrade = targetGrade.FullName,
                                StudentsPromoted = studentsToPromote.Count,
                                Message = $"Promoted {studentsToPromote.Count} students from {grade.FullName} to {targetGrade.FullName}",
                                CurriculumTransition = grade.CurriculumType != targetGrade.CurriculumType ? 
                                    $"{grade.CurriculumType} → {targetGrade.CurriculumType}" : null
                            });
                        }
                        else
                        {
                            summary.FailedPromotions++;
                            summary.Errors.Add($"{grade.FullName}: {GetPromotionErrorMessage(grade, currentAcademicYear)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        summary.FailedPromotions++;
                        summary.Errors.Add($"{grade.FullName}: Unexpected error - {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                summary.Success = summary.FailedPromotions == 0;
                summary.Message = summary.Success 
                    ? $"Successfully promoted students from {summary.SuccessfulPromotions} grades. Total students promoted: {summary.TotalStudentsPromoted}"
                    : $"Promotion completed with {summary.FailedPromotions} failures out of {summary.TotalGradesProcessed} grades.";

                return summary;
            }
            catch (Exception ex)
            {
                summary.Success = false;
                summary.Message = $"Critical error during bulk promotion: {ex.Message}";
                return summary;
            }
        }

        public async Task<List<Grade>> GetAvailablePromotionTargetsAsync(int fromGradeId, int currentAcademicYear)
        {
            var fromGrade = await _context.Grades.FindAsync(fromGradeId);
            if (fromGrade == null) return new List<Grade>();

            var targetGrade = await FindNextGradeAsync(fromGrade, currentAcademicYear);
            return targetGrade != null ? new List<Grade> { targetGrade } : new List<Grade>();
        }

        public async Task<TransitionStatusResult> GetTransitionStatusAsync(int academicYear)
        {
            var status = new TransitionStatusResult
            {
                AcademicYear = academicYear,
                IsTransitionActive = academicYear >= 2025 && academicYear <= 2030
            };

            // Get transition statistics
            var legacyGrades = await _context.Grades
                .Where(g => g.CurriculumType == CurriculumType.Legacy && g.IsActive)
                .CountAsync();

            var competencyGrades = await _context.Grades
                .Where(g => g.CurriculumType == CurriculumType.CompetencyBased && 
                           g.IsValidForYear(academicYear))
                .CountAsync();

            var transitionalGrades = await _context.Grades
                .Where(g => g.IsTransitional && g.IsValidForYear(academicYear))
                .CountAsync();

            status.LegacyGradesActive = legacyGrades;
            status.CompetencyGradesActive = competencyGrades;
            status.TransitionalGradesActive = transitionalGrades;

            // Determine transition phase
            if (academicYear < 2025)
                status.TransitionPhase = "Pre-Transition";
            else if (academicYear >= 2030)
                status.TransitionPhase = "Post-Transition (Fully CompetencyBased)";
            else
                status.TransitionPhase = $"Transition Year {academicYear - 2024} of 6";

            return status;
        }

        private async Task<Grade?> FindNextGradeAsync(Grade currentGrade, int currentAcademicYear)
        {
            // Handle Grade 6 → Form 1 transition (CompetencyBased system)
            if (currentGrade.Name == "Grade 6" && currentGrade.CurriculumType == CurriculumType.Legacy)
            {
                return await FindCompetencyBasedTarget(currentGrade, "Form 1", currentAcademicYear);
            }

            // Handle Grade 7 → Grade 8 (Legacy system continuation)
            if (currentGrade.Name == "Grade 7" && currentGrade.CurriculumType == CurriculumType.Legacy)
            {
                return await FindLegacyTarget(currentGrade, "Grade 8", currentAcademicYear);
            }

            // Handle normal progression within same curriculum
            var nextLevel = currentGrade.Level + 1;
            
            if (currentGrade.CurriculumType == CurriculumType.Legacy)
            {
                return await _context.Grades
                    .FirstOrDefaultAsync(g => 
                        g.Level == nextLevel &&
                        g.Stream == MapStreamForPromotion(currentGrade.Stream, currentGrade.Section, SchoolSection.Secondary) &&
                        g.Section == GetNextSection(currentGrade.Section) &&
                        g.CurriculumType == CurriculumType.Legacy &&
                        g.IsActive &&
                        g.IsValidForYear(currentAcademicYear));
            }
            else // CompetencyBased
            {
                return await _context.Grades
                    .FirstOrDefaultAsync(g => 
                        g.Level == nextLevel &&
                        g.Stream == currentGrade.Stream &&
                        g.Section == currentGrade.Section &&
                        g.CurriculumType == CurriculumType.CompetencyBased &&
                        g.IsActive &&
                        g.IsValidForYear(currentAcademicYear));
            }
        }

        private async Task<Grade?> FindCompetencyBasedTarget(Grade currentGrade, string targetGradeName, int currentAcademicYear)
        {
            var targetStream = MapStreamForPromotion(currentGrade.Stream, currentGrade.Section, SchoolSection.Secondary);
            
            return await _context.Grades
                .FirstOrDefaultAsync(g => 
                    g.Name == targetGradeName &&
                    g.Stream == targetStream &&
                    g.Section == SchoolSection.Secondary &&
                    g.CurriculumType == CurriculumType.CompetencyBased &&
                    g.IsActive &&
                    g.IsValidForYear(currentAcademicYear));
        }

        private async Task<Grade?> FindLegacyTarget(Grade currentGrade, string targetGradeName, int currentAcademicYear)
        {
            var targetStream = MapStreamForPromotion(currentGrade.Stream, currentGrade.Section, SchoolSection.Secondary);
            
            return await _context.Grades
                .FirstOrDefaultAsync(g => 
                    g.Name == targetGradeName &&
                    g.Stream == targetStream &&
                    g.Section == SchoolSection.Secondary &&
                    g.CurriculumType == CurriculumType.Legacy &&
                    g.IsActive &&
                    g.IsValidForYear(currentAcademicYear));
        }

        private string MapStreamForPromotion(string currentStream, SchoolSection currentSection, SchoolSection targetSection)
        {
            // If staying in same section, keep same stream
            if (currentSection == targetSection)
                return currentStream;

            // Primary to Secondary stream mapping
            if (currentSection == SchoolSection.Primary && targetSection == SchoolSection.Secondary)
            {
                return currentStream switch
                {
                    "Purple" => "Grey",
                    "Green" => "Blue",
                    "Orange" => "Grey", // Default mapping for Orange
                    _ => "Grey" // Default fallback
                };
            }

            return currentStream; // Default: no change
        }

        private SchoolSection GetNextSection(SchoolSection currentSection)
        {
            return currentSection switch
            {
                SchoolSection.Preschool => SchoolSection.Primary,
                SchoolSection.Primary => SchoolSection.Secondary,
                SchoolSection.Secondary => SchoolSection.Secondary, // Stay in secondary
                _ => currentSection
            };
        }

        private string GetPromotionErrorMessage(Grade grade, int currentAcademicYear)
        {
            if (grade.Name == "Grade 12" || grade.Name == "Form 6")
                return "Student has completed the highest grade level.";

            if (grade.IsTransitional && !grade.IsValidForYear(currentAcademicYear))
                return $"Grade {grade.FullName} is no longer available for the {currentAcademicYear} academic year.";

            return $"No suitable promotion target found for {grade.FullName} in {currentAcademicYear}.";
        }
    }

    // Result classes
    public class PromotionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public int StudentsPromoted { get; set; }
        public string? FromGrade { get; set; }
        public string? ToGrade { get; set; }
    }

    public class PromotionSummaryResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalGradesProcessed { get; set; }
        public int SuccessfulPromotions { get; set; }
        public int FailedPromotions { get; set; }
        public int TotalStudentsPromoted { get; set; }
        public List<GradePromotionDetail> PromotionDetails { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class GradePromotionDetail
    {
        public string FromGrade { get; set; } = string.Empty;
        public string ToGrade { get; set; } = string.Empty;
        public int StudentsPromoted { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? CurriculumTransition { get; set; } // e.g., "Legacy → CompetencyBased"
    }

    public class TransitionStatusResult
    {
        public int AcademicYear { get; set; }
        public bool IsTransitionActive { get; set; }
        public string TransitionPhase { get; set; } = string.Empty;
        public int LegacyGradesActive { get; set; }
        public int CompetencyGradesActive { get; set; }
        public int TransitionalGradesActive { get; set; }
    }
}