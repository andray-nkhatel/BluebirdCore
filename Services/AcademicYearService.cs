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
        Task<bool> PromoteAllStudentsAsync(int academicYearId);
        Task<bool> ArchiveGraduatesAsync(int academicYearId);
    }

    public class AcademicYearService : IAcademicYearService
    {
        private readonly SchoolDbContext _context;
        private readonly IStudentService _studentService;

        public AcademicYearService(SchoolDbContext context, IStudentService studentService)
        {
            _context = context;
            _studentService = studentService;
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

        public async Task<bool> PromoteAllStudentsAsync(int academicYearId)
        {
            var grades = await _context.Grades
                .Where(g => g.IsActive && g.Level < 12)
                .OrderBy(g => g.Level)
                .ToListAsync();

            foreach (var grade in grades)
            {
                var nextGrade = await _context.Grades
                    .FirstOrDefaultAsync(g => g.Level == grade.Level + 1 && g.IsActive);

                if (nextGrade != null)
                {
                    await _studentService.PromoteStudentsAsync(grade.Id, nextGrade.Id);
                }
            }

            return true;
        }

        public async Task<bool> ArchiveGraduatesAsync(int academicYearId)
        {
            var graduates = await _context.Students
                .Include(s => s.Grade)
                .Where(s => s.Grade.Level == 12 && !s.IsArchived)
                .ToListAsync();

            foreach (var graduate in graduates)
            {
                await _studentService.ArchiveStudentAsync(graduate.Id);
            }

            return true;
        }
    }
}

