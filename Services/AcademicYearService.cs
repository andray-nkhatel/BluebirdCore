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
        Task<AcademicYear> GetAcademicYearByIdAsync(int id);
        Task<AcademicYear> UpdateAcademicYearAsync(AcademicYear academicYear);
        Task<bool> DeleteAcademicYearAsync(int id);
    }

    public class AcademicYearService : IAcademicYearService
    {
        private readonly SchoolDbContext _context;
        private readonly IStudentService _studentService;
        private readonly ILogger<AcademicYearService> _logger;

        public AcademicYearService(SchoolDbContext context, IStudentService studentService, ILogger<AcademicYearService> logger)
        {
            _context = context;
            _studentService = studentService;
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


/// <summary>
/// Get academic year by ID
/// </summary>
/// <param name="id">Academic year ID</param>
/// <returns>Academic year or null if not found</returns>
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

/// <summary>
/// Update an existing academic year
/// </summary>
/// <param name="academicYear">Academic year to update</param>
/// <returns>Updated academic year</returns>
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

/// <summary>
/// Delete an academic year
/// </summary>
/// <param name="id">Academic year ID to delete</param>
/// <returns>True if deleted successfully, false if not found</returns>
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
/// Validate academic year data
/// </summary>
/// <param name="academicYear">Academic year to validate</param>
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

