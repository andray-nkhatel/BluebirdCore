using BluebirdCore.Data;
using BluebirdCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace BluebirdCore.Services
{
    public interface IAcademicYearService
    {
        Task<IEnumerable<AcademicYear>> GetAllAcademicYearsAsync();
        Task<AcademicYear> GetActiveAcademicYearAsync();
        Task<AcademicYear> CreateAcademicYearAsync(AcademicYear academicYear);
        Task<bool> CloseAcademicYearAsync(int academicYearId);
        Task<PromotionSummaryResult> PromoteAllStudentsAsync(int academicYearId);
        // Task<PromotionSummaryResult> PromoteSelectedGradesAsync(int academicYearId, List<int> gradeIds);
        Task<bool> ArchiveGraduatesAsync(int academicYearId);
        Task<AcademicYear> GetAcademicYearByIdAsync(int id);
        Task<AcademicYear> UpdateAcademicYearAsync(AcademicYear academicYear);
        Task<bool> DeleteAcademicYearAsync(int id);
        Task<TransitionStatusResult> GetTransitionStatusAsync(int academicYearId);
    }

    public class AcademicYearService : IAcademicYearService
    {
        private readonly SchoolDbContext _context;
        private readonly IStudentService _studentService;
        private readonly ITransitionPromotionService _transitionPromotionService;
        private readonly ILogger<AcademicYearService> _logger;

        public AcademicYearService(
            SchoolDbContext context,
            IStudentService studentService,
            ITransitionPromotionService transitionPromotionService,
            ILogger<AcademicYearService> logger)
        {
            _context = context;
            _studentService = studentService;
            _transitionPromotionService = transitionPromotionService;
            _logger = logger;
        }

        public async Task<IEnumerable<AcademicYear>> GetAllAcademicYearsAsync()
        {
            return await _context.AcademicYears
                .OrderByDescending(ay => ay.StartDate)
                .ToListAsync();
        }

        public async Task<AcademicYear> GetActiveAcademicYearAsync()
        {
            return await _context.AcademicYears
                .FirstOrDefaultAsync(ay => ay.IsActive);
        }

        public async Task<AcademicYear> CreateAcademicYearAsync(AcademicYear academicYear)
        {
            // Deactivate current active academic year
            var currentActive = await GetActiveAcademicYearAsync();
            if (currentActive != null)
            {
                currentActive.IsActive = false;
            }

            academicYear.IsActive = true;
            _context.AcademicYears.Add(academicYear);
            await _context.SaveChangesAsync();

            return academicYear;
        }

        public async Task<bool> CloseAcademicYearAsync(int academicYearId)
        {
            var academicYear = await _context.AcademicYears.FindAsync(academicYearId);
            if (academicYear == null) return false;

            academicYear.IsClosed = true;
            academicYear.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<PromotionSummaryResult> PromoteAllStudentsAsync(int academicYearId)
        {
            try
            {
                var academicYear = await GetAcademicYearByIdAsync(academicYearId);
                if (academicYear == null)
                {
                    return new PromotionSummaryResult
                    {
                        Success = false,
                        Message = "Academic year not found."
                    };
                }

                // Extract the year from the academic year (assuming the year is in the name or start date)
                var currentYear = academicYear.StartDate.Year;

                // Use the TransitionPromotionService for proper curriculum transition handling
                var result = await _transitionPromotionService.PromoteAllStudentsAsync(academicYearId, currentYear);

                // // Update academic year status if promotions were successful
                // if (result.Success && result.TotalStudentsPromoted > 0)
                // {
                //     await UpdateAcademicYearPromotionStatusAsync(academicYearId);
                // }

                _logger.LogInformation(
                    "Promotion completed for Academic Year {AcademicYearId}. " +
                    "Success: {Success}, Students Promoted: {StudentsPromoted}, " +
                    "Successful Grades: {SuccessfulGrades}, Failed Grades: {FailedGrades}",
                    academicYearId, result.Success, result.TotalStudentsPromoted,
                    result.SuccessfulPromotions, result.FailedPromotions);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during bulk promotion for Academic Year {AcademicYearId}", academicYearId);
                return new PromotionSummaryResult
                {
                    Success = false,
                    Message = $"Critical error during bulk promotion: {ex.Message}"
                };
            }
        }

        // public async Task<PromotionSummaryResult> PromoteSelectedGradesAsync(int academicYearId, List<int> gradeIds)
        // {
        //     try
        //     {
        //         var academicYear = await GetAcademicYearByIdAsync(academicYearId);
        //         if (academicYear == null)
        //         {
        //             return new PromotionSummaryResult
        //             {
        //                 Success = false,
        //                 Message = "Academic year not found."
        //             };
        //         }

        //         if (gradeIds == null || !gradeIds.Any())
        //         {
        //             return new PromotionSummaryResult
        //             {
        //                 Success = false,
        //                 Message = "No grades selected for promotion."
        //             };
        //         }

        //         var currentYear = academicYear.StartDate.Year;

        //         // Use the TransitionPromotionService for selective promotion
        //         var result = await _transitionPromotionService.PromoteSelectedGradesAsync(academicYearId, gradeIds, currentYear);

        //         // Update academic year status if promotions were successful
        //         if (result.Success && result.TotalStudentsPromoted > 0)
        //         {
        //             await UpdateAcademicYearPromotionStatusAsync(academicYearId);
        //         }

        //         _logger.LogInformation(
        //             "Selective promotion completed for Academic Year {AcademicYearId}. " +
        //             "Grades: {GradeIds}, Students Promoted: {StudentsPromoted}",
        //             academicYearId, string.Join(",", gradeIds), result.TotalStudentsPromoted);

        //         return result;
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error during selective promotion for Academic Year {AcademicYearId}", academicYearId);
        //         return new PromotionSummaryResult
        //         {
        //             Success = false,
        //             Message = $"Error during selective promotion: {ex.Message}"
        //         };
        //     }
        // }

        public async Task<bool> ArchiveGraduatesAsync(int academicYearId)
        {
            try
            {
                // Get the IDs of graduation grades (both Legacy Grade 12 and Cambridge Form 6)
                var graduationGradeIds = await GetGraduationGradeIdsAsync();

                if (!graduationGradeIds.Any())
                {
                    _logger.LogWarning("No graduation grades found for Academic Year {AcademicYearId}", academicYearId);
                    return false;
                }

                // Archive students from graduation grades
                var graduates = await _context.Students
                    .Include(s => s.Grade)
                    .Where(s => !s.IsArchived && graduationGradeIds.Contains(s.GradeId))
                    .ToListAsync();

                var archivedCount = 0;
                var graduationDetails = new List<string>();

                foreach (var graduate in graduates)
                {
                    var success = await _studentService.ArchiveStudentAsync(graduate.Id);
                    if (success)
                    {
                        archivedCount++;
                        graduationDetails.Add($"{graduate.FirstName} {graduate.LastName} from {graduate.Grade.FullName}");
                    }
                }

                _logger.LogInformation(
                    "Archived {ArchivedCount} graduates for Academic Year {AcademicYearId}. Details: {GraduationDetails}",
                    archivedCount, academicYearId, string.Join(", ", graduationDetails));

                return archivedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving graduates for Academic Year {AcademicYearId}", academicYearId);
                throw;
            }
        }

        private async Task<List<int>> GetGraduationGradeIdsAsync()
        {   
            return await _context.Grades
                .Where(g => g.IsActive && 
                           ((g.Name == "Grade 12" && g.CurriculumType == CurriculumType.Legacy) ||
                            (g.Name == "Form 6" && g.CurriculumType == CurriculumType.CompetencyBased)))
                .Select(g => g.Id)
                .ToListAsync();
        }

        public async Task<TransitionStatusResult> GetTransitionStatusAsync(int academicYearId)
        {
            try
            {
                var academicYear = await GetAcademicYearByIdAsync(academicYearId);
                if (academicYear == null)
                {
                    throw new ArgumentException("Academic year not found");
                }

                var currentYear = academicYear.StartDate.Year;
                return await _transitionPromotionService.GetTransitionStatusAsync(currentYear);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transition status for Academic Year {AcademicYearId}", academicYearId);
                throw;
            }
        }

        public async Task<AcademicYear> GetAcademicYearByIdAsync(int id)
        {
            try
            {
                return await _context.AcademicYears
                    .FirstOrDefaultAsync(ay => ay.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving academic year with ID {Id}", id);
                throw;
            }
        }

        public async Task<AcademicYear> UpdateAcademicYearAsync(AcademicYear academicYear)
        {
            try
            {
                // Validate the academic year
                ValidateAcademicYear(academicYear);

                // Check if academic year exists
                var existingYear = await _context.AcademicYears
                    .FirstOrDefaultAsync(ay => ay.Id == academicYear.Id);

                if (existingYear == null)
                {
                    throw new ArgumentException($"Academic year with ID {academicYear.Id} not found");
                }

                // Check for duplicate name (excluding current record)
                var duplicateName = await _context.AcademicYears
                    .AnyAsync(ay => ay.Name == academicYear.Name && ay.Id != academicYear.Id);

                if (duplicateName)
                {
                    throw new InvalidOperationException($"Academic year with name '{academicYear.Name}' already exists");
                }

                // Check for overlapping date ranges (excluding current record)
                var overlapping = await _context.AcademicYears
                    .AnyAsync(ay => ay.Id != academicYear.Id &&
                                  ((academicYear.StartDate >= ay.StartDate && academicYear.StartDate <= ay.EndDate) ||
                                   (academicYear.EndDate >= ay.StartDate && academicYear.EndDate <= ay.EndDate) ||
                                   (academicYear.StartDate <= ay.StartDate && academicYear.EndDate >= ay.EndDate)));

                if (overlapping)
                {
                    throw new InvalidOperationException("Academic year dates overlap with an existing academic year");
                }

                // Update the entity
                existingYear.Name = academicYear.Name;
                existingYear.StartDate = academicYear.StartDate;
                existingYear.EndDate = academicYear.EndDate;

                _context.AcademicYears.Update(existingYear);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Academic year updated successfully. ID: {Id}, Name: {Name}",
                    existingYear.Id, existingYear.Name);

                return existingYear;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating academic year with ID {Id}", academicYear.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAcademicYearAsync(int id)
        {
            try
            {
                var academicYear = await _context.AcademicYears
                    .FirstOrDefaultAsync(ay => ay.Id == id);

                if (academicYear == null)
                {
                    _logger.LogWarning("Academic year with ID {Id} not found for deletion", id);
                    return false;
                }

                // Check if academic year is currently active
                if (academicYear.IsActive)
                {
                    throw new InvalidOperationException("Cannot delete an active academic year");
                }

                _context.AcademicYears.Remove(academicYear);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Academic year deleted successfully. ID: {Id}, Name: {Name}",
                    academicYear.Id, academicYear.Name);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting academic year with ID {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Update academic year promotion status
        /// </summary>
        // private async Task UpdateAcademicYearPromotionStatusAsync(int academicYearId)
        // {
        //     var academicYear = await _context.AcademicYears.FindAsync(academicYearId);
        //     if (academicYear != null)
        //     {
        //         academicYear.PromotionCompletedDate = DateTime.UtcNow;
        //         academicYear.LastModifiedDate = DateTime.UtcNow;
        //         await _context.SaveChangesAsync();
        //     }
        // }

        /// <summary>
        /// Validate academic year data
        /// </summary>
        private void ValidateAcademicYear(AcademicYear academicYear)
        {
            if (academicYear == null)
                throw new ArgumentNullException(nameof(academicYear));

            if (string.IsNullOrWhiteSpace(academicYear.Name))
                throw new ArgumentException("Academic year name is required");

            if (academicYear.StartDate >= academicYear.EndDate)
                throw new ArgumentException("Start date must be before end date");

            if (academicYear.EndDate <= DateTime.Today && academicYear.StartDate <= DateTime.Today)
                throw new ArgumentException("Academic year cannot be entirely in the past");
        }

    }
}