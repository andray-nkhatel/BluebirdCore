using BluebirdCore.Data;
using BluebirdCore.DTOs;
using BluebirdCore.Entities;
using BluebirdCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace BluebirdCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExamsController : ControllerBase
    {
        private readonly IExamService _examService;
        private readonly SchoolDbContext _context;
        private readonly ILogger<ExamsController> _logger;

        public ExamsController(IExamService examService, SchoolDbContext context, ILogger<ExamsController> logger)
        {
            _examService = examService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get exam scores for a student
        /// </summary>
        [HttpGet("student/{studentId}/scores")]
        [Authorize(Roles = "Admin,Teacher,Staff")]
        public async Task<ActionResult<IEnumerable<ExamScoreDto>>> GetStudentScores(int studentId, [FromQuery] int academicYear, [FromQuery] int term)
        {
            try
            {
                var scores = await _examService.GetScoresByStudentAsync(studentId, academicYear, term);
                var scoreDtos = scores.Select(s => new ExamScoreDto
                {
                    Id = s.Id,
                    StudentId = s.StudentId,
                    StudentName = s.Student?.FullName,
                    SubjectId = s.SubjectId,
                    SubjectName = s.Subject?.Name,
                    ExamTypeId = s.ExamTypeId,
                    ExamTypeName = s.ExamType?.Name,
                    Score = s.Score,
                    AcademicYear = s.AcademicYear,
                    Term = s.Term,
                    RecordedAt = s.RecordedAt,
                    RecordedByName = s.RecordedByTeacher?.FullName
                });

                return Ok(scoreDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student scores for StudentId: {StudentId}", studentId);
                return StatusCode(500, "An error occurred while retrieving student scores");
            }
        }

        /// <summary>
        /// Get exam scores for a grade/class
        /// </summary>
        [HttpGet("grade/{gradeId}/scores")]
        [Authorize(Roles = "Admin,Teacher,Staff")]
        public async Task<ActionResult<IEnumerable<ExamScoreDto>>> GetGradeScores(int gradeId, [FromQuery] int academicYear, [FromQuery] int term)
        {
            try
            {
                var scores = await _examService.GetScoresByGradeAsync(gradeId, academicYear, term);
                var scoreDtos = scores.Select(s => new ExamScoreDto
                {
                    Id = s.Id,
                    StudentId = s.StudentId,
                    StudentName = s.Student?.FullName,
                    SubjectId = s.SubjectId,
                    SubjectName = s.Subject?.Name,
                    ExamTypeId = s.ExamTypeId,
                    ExamTypeName = s.ExamType?.Name,
                    Score = s.Score,
                    AcademicYear = s.AcademicYear,
                    Term = s.Term,
                    RecordedAt = s.RecordedAt,
                    RecordedByName = s.RecordedByTeacher?.FullName
                });

                return Ok(scoreDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving grade scores for GradeId: {GradeId}", gradeId);
                return StatusCode(500, "An error occurred while retrieving grade scores");
            }
        }

        /// <summary>
        /// Create or update exam score (Teachers only)
        /// </summary>
        [HttpPost("scores")]
        [Authorize] // Remove role restriction temporarily
        public async Task<ActionResult<ExamScoreDto>> CreateOrUpdateScore([FromBody] CreateExamScoreDto scoreDto)
        {
            try
            {
                _logger.LogInformation("CreateOrUpdateScore called by user: {UserId}", User?.Identity?.Name);
                _logger.LogInformation("User authenticated: {IsAuthenticated}", User?.Identity?.IsAuthenticated);
                _logger.LogInformation("User role claims: {Claims}", string.Join(", ", User?.Claims?.Select(c => $"{c.Type}:{c.Value}") ?? new string[0]));

                // Manual role check since [Authorize(Roles = "Teacher")] isn't working
                var hasTeacherRole = User?.Claims?.Any(c => 
                    (c.Type == ClaimTypes.Role || c.Type == "role") && 
                    c.Value.Equals("Teacher", StringComparison.OrdinalIgnoreCase)) ?? false;

                if (!hasTeacherRole)
                {
                    _logger.LogWarning("User does not have Teacher role. Claims: {Claims}", 
                        string.Join(", ", User?.Claims?.Select(c => $"{c.Type}:{c.Value}") ?? new string[0]));
                    return StatusCode(403, "Teacher role required");
                }

                // Validate input
                if (scoreDto == null)
                {
                    _logger.LogWarning("CreateOrUpdateScore called with null scoreDto");
                    return BadRequest("Score data is required");
                }

                // Get teacher ID from claims with better error handling
                var teacherIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(teacherIdClaim) || !int.TryParse(teacherIdClaim, out int teacherId))
                {
                    _logger.LogError("Could not parse teacher ID from claims. Claim value: {ClaimValue}", teacherIdClaim);
                    return Unauthorized("Invalid teacher credentials");
                }

                _logger.LogInformation("Processing score entry for TeacherId: {TeacherId}, StudentId: {StudentId}, SubjectId: {SubjectId}", 
                    teacherId, scoreDto.StudentId, scoreDto.SubjectId);

                // Get student's grade to check authorization
                var student = await _context.Students.FindAsync(scoreDto.StudentId);
                if (student == null)
                {
                    _logger.LogWarning("Student not found with ID: {StudentId}", scoreDto.StudentId);
                    return NotFound("Student not found");
                }

                _logger.LogInformation("Found student: {StudentName}, GradeId: {GradeId}", student.FullName, student.GradeId);

                // Check if teacher can enter score for this subject/grade
                var canEnter = await _examService.CanTeacherEnterScore(teacherId, scoreDto.SubjectId, student.GradeId);
                if (!canEnter)
                {
                    _logger.LogWarning("Teacher {TeacherId} not authorized to enter scores for SubjectId: {SubjectId}, GradeId: {GradeId}", 
                        teacherId, scoreDto.SubjectId, student.GradeId);
                    return StatusCode(403, "You are not authorized to enter scores for this subject and grade combination");
                }

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

                _logger.LogInformation("Creating/updating exam score: {@ExamScore}", examScore);

                var createdScore = await _examService.CreateOrUpdateScoreAsync(examScore);

                _logger.LogInformation("Score created/updated successfully with ID: {ScoreId}", createdScore.Id);

                // Reload the entity with navigation properties for proper response
                var scoreWithNavigation = await _context.ExamScores
                    .Include(s => s.Student)
                    .Include(s => s.Subject)
                    .Include(s => s.ExamType)
                    .Include(s => s.RecordedByTeacher)
                    .FirstOrDefaultAsync(s => s.Id == createdScore.Id);

                if (scoreWithNavigation == null)
                {
                    _logger.LogError("Score was created but could not be retrieved with ID: {ScoreId}", createdScore.Id);
                    return StatusCode(500, "Score was created but could not be retrieved");
                }

                var responseDto = new ExamScoreDto
                {
                    Id = scoreWithNavigation.Id,
                    StudentId = scoreWithNavigation.StudentId,
                    StudentName = scoreWithNavigation.Student?.FullName,
                    SubjectId = scoreWithNavigation.SubjectId,
                    SubjectName = scoreWithNavigation.Subject?.Name,
                    ExamTypeId = scoreWithNavigation.ExamTypeId,
                    ExamTypeName = scoreWithNavigation.ExamType?.Name,
                    Score = scoreWithNavigation.Score,
                    AcademicYear = scoreWithNavigation.AcademicYear,
                    Term = scoreWithNavigation.Term,
                    RecordedAt = scoreWithNavigation.RecordedAt,
                    RecordedByName = scoreWithNavigation.RecordedByTeacher?.FullName
                };

                _logger.LogInformation("Returning score response: {@ResponseDto}", responseDto);

                return Ok(responseDto);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access in CreateOrUpdateScore");
                return Unauthorized("Access denied");
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument in CreateOrUpdateScore");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CreateOrUpdateScore");
                return StatusCode(500, "An unexpected error occurred while processing the request");
            }
        }

        /// <summary>
        /// Get all exam types
        /// </summary>
        [HttpGet("types")]
        [Authorize(Roles = "Admin,Teacher,Staff")]
        public async Task<ActionResult<IEnumerable<ExamType>>> GetExamTypes()
        {
            try
            {
                var examTypes = await _examService.GetExamTypesAsync();
                return Ok(examTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving exam types");
                return StatusCode(500, "An error occurred while retrieving exam types");
            }
        }

        /// <summary>
        /// Create new exam type (Admin only)
        /// </summary>
        [HttpPost("types")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ExamType>> CreateExamType([FromBody] CreateExamTypeDto createExamTypeDto)
        {
            try
            {
                if (createExamTypeDto == null)
                    return BadRequest("Exam type data is required");

                var examType = new ExamType
                {
                    Name = createExamTypeDto.Name,
                    Description = createExamTypeDto.Description,
                    Order = createExamTypeDto.Order
                };

                var createdExamType = await _examService.CreateExamTypeAsync(examType);
                return CreatedAtAction(nameof(GetExamTypes), new { id = createdExamType.Id }, createdExamType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating exam type");
                return StatusCode(500, "An error occurred while creating the exam type");
            }
        }

        /// <summary>
        /// Get teacher's assigned subjects and grades
        /// </summary>
        [HttpGet("teacher/assignments")]
        [Authorize(Roles = "Teacher")]
        public async Task<ActionResult<IEnumerable<TeacherAssignmentDto>>> GetTeacherAssignments()
        {
            try
            {
                var teacherIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(teacherIdClaim) || !int.TryParse(teacherIdClaim, out int teacherId))
                {
                    return Unauthorized("Invalid teacher credentials");
                }

                var assignments = await _context.TeacherSubjectAssignments
                    .Include(tsa => tsa.Subject)
                    .Include(tsa => tsa.Grade)
                    .Where(tsa => tsa.TeacherId == teacherId && tsa.IsActive)
                    .ToListAsync();

                var assignmentDtos = assignments.Select(a => new TeacherAssignmentDto
                {
                    Id = a.Id,
                    SubjectId = a.SubjectId,
                    SubjectName = a.Subject?.Name,
                    GradeId = a.GradeId,
                    GradeName = a.Grade?.FullName,
                    AssignedAt = a.AssignedAt
                });

                return Ok(assignmentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving teacher assignments");
                return StatusCode(500, "An error occurred while retrieving teacher assignments");
            }
        }

        /// <summary>
        /// Debug endpoint to check authentication status - NO ROLE RESTRICTION for debugging
        /// </summary>
        [HttpGet("debug/auth")]
        [Authorize] // Remove role restriction temporarily for debugging
        public ActionResult<object> DebugAuth()
        {
            return Ok(new
            {
                IsAuthenticated = User?.Identity?.IsAuthenticated,
                Identity = User?.Identity?.Name,
                Claims = User?.Claims?.Select(c => new { c.Type, c.Value }).ToList(),
                TeacherId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                // Check different role claim types
                RoleClaimStandard = User?.FindFirst(ClaimTypes.Role)?.Value,
                RoleClaimDirect = User?.FindFirst("role")?.Value,
                RoleClaimHttp = User?.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value,
                AllRoleClaims = User?.Claims?.Where(c => c.Type.Contains("role", StringComparison.OrdinalIgnoreCase))
                    .Select(c => new { c.Type, c.Value }).ToList()
            });
        }

        /// <summary>
        /// Alternative debug endpoint that checks roles manually
        /// </summary>
        [HttpGet("debug/roles")]
        [Authorize]
        public ActionResult<object> DebugRoles()
        {
            var hasTeacherRole = User?.IsInRole("Teacher");
            var hasTeacherRoleManual = User?.Claims?.Any(c => 
                (c.Type == ClaimTypes.Role || c.Type == "role") && 
                c.Value.Equals("Teacher", StringComparison.OrdinalIgnoreCase));

            return Ok(new
            {
                HasTeacherRoleBuiltIn = hasTeacherRole,
                HasTeacherRoleManual = hasTeacherRoleManual,
                AllClaims = User?.Claims?.Select(c => new { c.Type, c.Value }).ToList()
            });
        }
    }
}