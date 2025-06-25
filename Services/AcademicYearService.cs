using BluebirdCore.Data;
using BluebirdCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BluebirdCore.Services
{
    public interface IAcademicYearService
    {
        Task<IEnumerable<AcademicYear>> GetAllAcademicYearsAsync();
        Task<AcademicYear> GetActiveAcademicYearAsync();
        Task<AcademicYear> CreateAcademicYearAsync(AcademicYear academicYear);
        Task<bool> CloseAcademicYearAsync(int academicYearId);
        Task<int> PromoteStudentsPreservingStreamAsync();
        Task<AcademicYear?> GetAcademicYearByIdAsync(int academicYearId);
        Task<bool> DeleteAcademicYearAsync(int academicYearId);
        Task<bool> UpdateAcademicYearAsync(AcademicYear academicYear);
    }

    public class AcademicYearService : IAcademicYearService
    {
        private readonly SchoolDbContext _context;
        private readonly ILogger<AcademicYearService> _logger;

        public AcademicYearService(
            SchoolDbContext context,
            ILogger<AcademicYearService> logger)
        {
            _context = context;
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

        public async Task<AcademicYear?> GetAcademicYearByIdAsync(int academicYearId)
        {
            return await _context.AcademicYears
                .FirstOrDefaultAsync(ay => ay.Id == academicYearId);
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

        public async Task<int> PromoteStudentsPreservingStreamAsync()
        {
            // Load all grades into memory for quick lookup
            var grades = await _context.Grades
                .Where(g => g.IsActive)
                .ToListAsync();

            // Group grades by (Section, Stream), and order by Level
            var gradesBySectionStream = grades
                .GroupBy(g => new { g.Section, g.Stream })
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.Level).ToList()
                );

            // Get all active students
            var students = await _context.Students
                .Where(s => s.IsActive && !s.IsArchived)
                .Include(s => s.Grade)
                .ToListAsync();

            int promotedCount = 0;

            foreach (var student in students)
            {
                var currentGrade = student.Grade;
                if (currentGrade == null) continue;

                var key = new { currentGrade.Section, currentGrade.Stream };
                if (!gradesBySectionStream.TryGetValue(key, out var gradeList))
                    continue;

                // Find the index of the student's current grade in the ordered list
                int idx = gradeList.FindIndex(g => g.Id == currentGrade.Id);
                if (idx >= 0 && idx < gradeList.Count - 1)
                {
                    // Promote to the next grade in the same stream and section
                    var nextGrade = gradeList[idx + 1];
                    student.GradeId = nextGrade.Id;
                    promotedCount++;
                }
                // else: student is in the final grade of their stream/section, do not promote
            }

            await _context.SaveChangesAsync();
            return promotedCount;
        }

        public async Task<bool> DeleteAcademicYearAsync(int academicYearId)
        {
            var academicYear = await _context.AcademicYears.FindAsync(academicYearId);
            if (academicYear == null)
                return false;

            // Optionally: Check for related data before deleting
            _context.AcademicYears.Remove(academicYear);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetActiveAcademicYearAsync(int academicYearId)
        {
            var academicYear = await _context.AcademicYears.FindAsync(academicYearId);
            if (academicYear == null)
                return false;

            // Deactivate all other academic years
            var allYears = await _context.AcademicYears.ToListAsync();
            foreach (var ay in allYears)
            {
                ay.IsActive = ay.Id == academicYearId;
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAcademicYearAsync(AcademicYear academicYear)
        {
            var existing = await _context.AcademicYears.FindAsync(academicYear.Id);
            if (existing == null)
                return false;

            existing.Name = academicYear.Name;
            existing.StartDate = academicYear.StartDate;
            existing.EndDate = academicYear.EndDate;
            existing.IsActive = academicYear.IsActive;
            existing.IsClosed = academicYear.IsClosed;
            // Add other fields as needed

            await _context.SaveChangesAsync();
            return true;
        }


    }
}