using BluebirdCore.Data;
using BluebirdCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace BluebirdCore.Services
{
    public interface IExamService
    {
        Task<IEnumerable<ExamScore>> GetScoresByStudentAsync(int studentId, int academicYear, int term);
        Task<IEnumerable<ExamScore>> GetScoresByGradeAsync(int gradeId, int academicYear, int term);
        Task<ExamScore> CreateOrUpdateScoreAsync(ExamScore score);
        Task<bool> CanTeacherEnterScore(int teacherId, int subjectId, int gradeId);
        Task<IEnumerable<ExamType>> GetExamTypesAsync();
        Task<ExamType> CreateExamTypeAsync(ExamType examType);
    }

    public class ExamService : IExamService
    {
        private readonly SchoolDbContext _context;

        public ExamService(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ExamScore>> GetScoresByStudentAsync(int studentId, int academicYear, int term)
        {
            return await _context.ExamScores
                .Include(es => es.Subject)
                .Include(es => es.ExamType)
                .Where(es => es.StudentId == studentId && es.AcademicYear == academicYear && es.Term == term)
                .OrderBy(es => es.Subject.Name)
                .ThenBy(es => es.ExamType.Order)
                .ToListAsync();
        }

        public async Task<IEnumerable<ExamScore>> GetScoresByGradeAsync(int gradeId, int academicYear, int term)
        {
            return await _context.ExamScores
                .Include(es => es.Student)
                .Include(es => es.Subject)
                .Include(es => es.ExamType)
                .Where(es => es.GradeId == gradeId && es.AcademicYear == academicYear && es.Term == term)
                .OrderBy(es => es.Student.LastName)
                .ThenBy(es => es.Student.FirstName)
                .ThenBy(es => es.Subject.Name)
                .ToListAsync();
        }

        public async Task<ExamScore> CreateOrUpdateScoreAsync(ExamScore score)
        {
            var existingScore = await _context.ExamScores
                .FirstOrDefaultAsync(es => es.StudentId == score.StudentId 
                                         && es.SubjectId == score.SubjectId 
                                         && es.ExamTypeId == score.ExamTypeId 
                                         && es.AcademicYear == score.AcademicYear 
                                         && es.Term == score.Term);

            if (existingScore != null)
            {
                existingScore.Score = score.Score;
                existingScore.RecordedAt = DateTime.UtcNow;
                existingScore.RecordedBy = score.RecordedBy;
            }
            else
            {
                score.RecordedAt = DateTime.UtcNow;
                _context.ExamScores.Add(score);
            }

            await _context.SaveChangesAsync();
            return existingScore ?? score;
        }

        public async Task<bool> CanTeacherEnterScore(int teacherId, int subjectId, int gradeId)
        {
            return await _context.TeacherSubjectAssignments
                .AnyAsync(tsa => tsa.TeacherId == teacherId 
                              && tsa.SubjectId == subjectId 
                              && tsa.GradeId == gradeId 
                              && tsa.IsActive);
        }

        public async Task<IEnumerable<ExamType>> GetExamTypesAsync()
        {
            return await _context.ExamTypes
                .Where(et => et.IsActive)
                .OrderBy(et => et.Order)
                .ToListAsync();
        }

        public async Task<ExamType> CreateExamTypeAsync(ExamType examType)
        {
            _context.ExamTypes.Add(examType);
            await _context.SaveChangesAsync();
            return examType;
        }
    }
}