using BluebirdCore.Data;
using BluebirdCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace BluebirdCore.Services
{
    public interface IStudentService
    {
        Task<IEnumerable<Student>> GetAllStudentsAsync(bool includeArchived = false);
        Task<IEnumerable<Student>> GetStudentsByGradeAsync(int gradeId);
        Task<Student> GetStudentByIdAsync(int id);
        Task<Student> CreateStudentAsync(Student student);
        Task<Student> UpdateStudentAsync(Student student);
        Task<bool> DeleteStudentAsync(int id);
        Task<bool> ArchiveStudentAsync(int id);
        Task<bool> PromoteStudentsAsync(int fromGradeId, int toGradeId);
        Task<IEnumerable<Student>> ImportStudentsFromCsvAsync(Stream csvStream);
    }

    public class StudentService : IStudentService
    {
        private readonly SchoolDbContext _context;

        public StudentService(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Student>> GetAllStudentsAsync(bool includeArchived = false)
        {
            var query = _context.Students.Include(s => s.Grade).AsQueryable();

            if (!includeArchived)
                query = query.Where(s => !s.IsArchived);

            return await query.OrderBy(s => s.LastName)
                             .ThenBy(s => s.FirstName)
                             .ToListAsync();
        }

        public async Task<IEnumerable<Student>> GetStudentsByGradeAsync(int gradeId)
        {
            return await _context.Students
                .Include(s => s.Grade)
                .Where(s => s.GradeId == gradeId && !s.IsArchived)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }

        public async Task<Student> GetStudentByIdAsync(int id)
        {
            return await _context.Students
                .Include(s => s.Grade)
                .Include(s => s.OptionalSubjects)
                    .ThenInclude(os => os.Subject)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Student> CreateStudentAsync(Student student)
        {
            // Generate student number if not provided
            if (string.IsNullOrEmpty(student.StudentNumber))
            {
                student.StudentNumber = await GenerateStudentNumberAsync();
            }

            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            return student;
        }

        public async Task<Student> UpdateStudentAsync(Student student)
        {
            _context.Entry(student).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return student;
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return false;

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ArchiveStudentAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return false;

            student.IsArchived = true;
            student.ArchiveDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PromoteStudentsAsync(int fromGradeId, int toGradeId)
        {
            var students = await _context.Students
                .Where(s => s.GradeId == fromGradeId && !s.IsArchived)
                .ToListAsync();

            foreach (var student in students)
            {
                student.GradeId = toGradeId;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Student>> ImportStudentsFromCsvAsync(Stream csvStream)
        {
            // Implementation for CSV import would go here
            // This is a placeholder - you'd use a CSV parsing library
            throw new NotImplementedException("CSV import functionality to be implemented");
        }

        private async Task<string> GenerateStudentNumberAsync()
        {
            var year = DateTime.Now.Year.ToString().Substring(2);
            var lastStudent = await _context.Students
                .Where(s => s.StudentNumber.StartsWith(year))
                .OrderByDescending(s => s.StudentNumber)
                .FirstOrDefaultAsync();

            var nextNumber = 1;
            if (lastStudent != null && int.TryParse(lastStudent.StudentNumber.Substring(2), out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }

            return $"{year}{nextNumber:D4}";
        }
    }
}