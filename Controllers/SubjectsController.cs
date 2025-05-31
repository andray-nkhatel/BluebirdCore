using BluebirdCore.Data;
using BluebirdCore.DTOs;
using BluebirdCore.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BluebirdCore.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubjectsController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public SubjectsController(SchoolDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all subjects
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher,Staff")]
        public async Task<ActionResult<IEnumerable<SubjectDto>>> GetSubjects()
        {
            var subjects = await _context.Subjects
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            var subjectDtos = subjects.Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Description = s.Description,
                IsActive = s.IsActive
            });

            return Ok(subjectDtos);
        }

        /// <summary>
        /// Get subject by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Teacher,Staff")]
        public async Task<ActionResult<SubjectDto>> GetSubject(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return NotFound();

            return Ok(new SubjectDto
            {
                Id = subject.Id,
                Name = subject.Name,
                Code = subject.Code,
                Description = subject.Description,
                IsActive = subject.IsActive
            });
        }

        /// <summary>
        /// Create new subject (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SubjectDto>> CreateSubject([FromBody] CreateSubjectDto createSubjectDto)
        {
            var subject = new Subject
            {
                Name = createSubjectDto.Name,
                Code = createSubjectDto.Code,
                Description = createSubjectDto.Description
            };

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSubject), new { id = subject.Id }, new SubjectDto
            {
                Id = subject.Id,
                Name = subject.Name,
                Code = subject.Code,
                Description = subject.Description,
                IsActive = subject.IsActive
            });
        }

        /// <summary>
        /// Assign subject to grade (Admin only)
        /// </summary>
        [HttpPost("{subjectId}/assign-to-grade/{gradeId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AssignSubjectToGrade(int subjectId, int gradeId, [FromBody] AssignSubjectToGradeDto assignDto)
        {
            var existingAssignment = await _context.GradeSubjects
                .FirstOrDefaultAsync(gs => gs.SubjectId == subjectId && gs.GradeId == gradeId);

            if (existingAssignment != null)
                return BadRequest(new { message = "Subject already assigned to this grade" });

            var gradeSubject = new GradeSubject
            {
                SubjectId = subjectId,
                GradeId = gradeId,
                IsOptional = assignDto.IsOptional
            };

            _context.GradeSubjects.Add(gradeSubject);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Subject assigned to grade successfully" });
        }

        /// <summary>
        /// Assign teacher to subject for specific grade (Admin only)
        /// </summary>
        [HttpPost("{subjectId}/assign-teacher")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AssignTeacherToSubject(int subjectId, [FromBody] AssignTeacherToSubjectDto assignDto)
        {
            var existingAssignment = await _context.TeacherSubjectAssignments
                .FirstOrDefaultAsync(tsa => tsa.TeacherId == assignDto.TeacherId 
                                         && tsa.SubjectId == subjectId 
                                         && tsa.GradeId == assignDto.GradeId
                                         && tsa.IsActive);

            if (existingAssignment != null)
                return BadRequest(new { message = "Teacher already assigned to this subject for this grade" });

            var assignment = new TeacherSubjectAssignment
            {
                TeacherId = assignDto.TeacherId,
                SubjectId = subjectId,
                GradeId = assignDto.GradeId
            };

            _context.TeacherSubjectAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Teacher assigned to subject successfully" });
        }

        /// <summary>
        /// Bulk import subjects from CSV (Admin only)
        /// </summary>
        [HttpPost("import/csv")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ImportSubjectsFromCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please select a valid CSV file" });

            try
            {
                // Implementation for CSV import would go here
                return Ok(new { message = "CSV import functionality to be implemented" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}