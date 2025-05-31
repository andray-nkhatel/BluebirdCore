using BluebirdCore.Data;
using BluebirdCore.DTOs;
using BluebirdCore.Entities;
using BluebirdCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BluebirdCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExamsController : ControllerBase
    {
        private readonly IExamService _examService;
        private readonly SchoolDbContext _context;

        public ExamsController(IExamService examService, SchoolDbContext context)
        {
            _examService = examService;
            _context = context;
        }

        /// <summary>
        /// Get exam scores for a student
        /// </summary>
        [HttpGet("student/{studentId}/scores")]
        [Authorize(Roles = "Admin,Teacher,Staff")]
        public async Task<ActionResult<IEnumerable<ExamScoreDto>>> GetStudentScores(int studentId, [FromQuery] int academicYear, [FromQuery] int term)
        {
            var scores = await _examService.GetScoresByStudentAsync(studentId, academicYear, term);
            var scoreDtos = scores.Select(s => new ExamScoreDto
            {
                Id = s.Id,
                StudentId = s.StudentId,
                StudentName = s.Student.FullName,
                SubjectId = s.SubjectId,
                SubjectName = s.Subject.Name,
                ExamTypeId = s.ExamTypeId,
                ExamTypeName = s.ExamType.Name,
                Score = s.Score,
                AcademicYear = s.AcademicYear,
                Term = s.Term,
                RecordedAt = s.RecordedAt,
                RecordedByName = s.RecordedByTeacher.FullName
            });

            return Ok(scoreDtos);
        }

        /// <summary>
        /// Get exam scores for a grade/class
        /// </summary>
        [HttpGet("grade/{gradeId}/scores")]
        [Authorize(Roles = "Admin,Teacher,Staff")]
        public async Task<ActionResult<IEnumerable<ExamScoreDto>>> GetGradeScores(int gradeId, [FromQuery] int academicYear, [FromQuery] int term)
        {
            var scores = await _examService.GetScoresByGradeAsync(gradeId, academicYear, term);
            var scoreDtos = scores.Select(s => new ExamScoreDto
            {
                Id = s.Id,
                StudentId = s.StudentId,
                StudentName = s.Student.FullName,
                SubjectId = s.SubjectId,
                SubjectName = s.Subject.Name,
                ExamTypeId = s.ExamTypeId,
                ExamTypeName = s.ExamType.Name,
                Score = s.Score,
                AcademicYear = s.AcademicYear,
                Term = s.Term,
                RecordedAt = s.RecordedAt,
                RecordedByName = s.RecordedByTeacher.FullName
            });

            return Ok(scoreDtos);
        }

        /// <summary>
        /// Create or update exam score (Teachers only)
        /// </summary>
        [HttpPost("scores")]
        [Authorize(Roles = "Teacher")]
        public async Task<ActionResult<ExamScoreDto>> CreateOrUpdateScore([FromBody] CreateExamScoreDto scoreDto)
        {
            var teacherId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
            
            // Get student's grade to check authorization
            var student = await _context.Students.FindAsync(scoreDto.StudentId);
            if (student == null)
                return NotFound("Student not found");

            // Check if teacher can enter score for this subject/grade
            var canEnter = await _examService.CanTeacherEnterScore(teacherId, scoreDto.SubjectId, student.GradeId);
            if (!canEnter)
                return Forbid("You are not authorized to enter scores for this subject");

            var examScore = new ExamScore
            {
                StudentId = scoreDto.StudentId,
                SubjectId = scoreDto.SubjectId,
                ExamTypeId = scoreDto.ExamTypeId,
                GradeId = student.GradeId,
                Score = scoreDto.Score,
                AcademicYear = scoreDto.AcademicYear,
                Term = scoreDto.Term,
                RecordedBy = teacherId
            };

            var createdScore = await _examService.CreateOrUpdateScoreAsync(examScore);

            return Ok(new ExamScoreDto
            {
                Id = createdScore.Id,
                StudentId = createdScore.StudentId,
                SubjectId = createdScore.SubjectId,
                ExamTypeId = createdScore.ExamTypeId,
                Score = createdScore.Score,
                AcademicYear = createdScore.AcademicYear,
                Term = createdScore.Term,
                RecordedAt = createdScore.RecordedAt
            });
        }

        /// <summary>
        /// Get all exam types
        /// </summary>
        [HttpGet("types")]
        [Authorize(Roles = "Admin,Teacher,Staff")]
        public async Task<ActionResult<IEnumerable<ExamType>>> GetExamTypes()
        {
            var examTypes = await _examService.GetExamTypesAsync();
            return Ok(examTypes);
        }

        /// <summary>
        /// Create new exam type (Admin only)
        /// </summary>
        [HttpPost("types")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ExamType>> CreateExamType([FromBody] CreateExamTypeDto createExamTypeDto)
        {
            var examType = new ExamType
            {
                Name = createExamTypeDto.Name,
                Description = createExamTypeDto.Description,
                Order = createExamTypeDto.Order
            };

            var createdExamType = await _examService.CreateExamTypeAsync(examType);
            return CreatedAtAction(nameof(GetExamTypes), new { id = createdExamType.Id }, createdExamType);
        }

        /// <summary>
        /// Get teacher's assigned subjects and grades
        /// </summary>
        [HttpGet("teacher/assignments")]
        [Authorize(Roles = "Teacher")]
        public async Task<ActionResult<IEnumerable<TeacherAssignmentDto>>> GetTeacherAssignments()
        {
            var teacherId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
            
            var assignments = await _context.TeacherSubjectAssignments
                .Include(tsa => tsa.Subject)
                .Include(tsa => tsa.Grade)
                .Where(tsa => tsa.TeacherId == teacherId && tsa.IsActive)
                .ToListAsync();

            var assignmentDtos = assignments.Select(a => new TeacherAssignmentDto
            {
                Id = a.Id,
                SubjectId = a.SubjectId,
                SubjectName = a.Subject.Name,
                GradeId = a.GradeId,
                GradeName = a.Grade.FullName,
                AssignedAt = a.AssignedAt
            });

            return Ok(assignmentDtos);
        }
    }
}