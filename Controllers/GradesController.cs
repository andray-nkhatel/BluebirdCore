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
    public class GradesController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public GradesController(SchoolDbContext context)
        {
            _context = context;
        }

     [HttpGet]
[Authorize(Roles = "Admin,Teacher,Staff")]
public async Task<ActionResult<IEnumerable<GradeDto>>> GetGrades([FromQuery] bool includeInactive = false)
{
    var grades = await _context.Grades
        .Include(g => g.HomeroomTeacher)
        .Include(g => g.Students)
        .Where(g => includeInactive || g.IsActive) // Single condition
        .OrderBy(g => g.Level)
        .ThenBy(g => g.Stream)
        .ToListAsync();

    var gradeDtos = grades.Select(g => new GradeDto
    {
        Id = g.Id,
        Name = g.Name,
        Stream = g.Stream,
        FullName = g.FullName,
        Level = g.Level,
        Section = g.Section.ToString(),
        HomeroomTeacherId = g.HomeroomTeacherId,
        HomeroomTeacherName = g.HomeroomTeacher?.FullName,
        IsActive = g.IsActive,
        StudentCount = g.Students.Count(s => !s.IsArchived)
    });

    return Ok(gradeDtos);
}
        /// <summary>
        /// Get grade by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Teacher,Staff")]
        public async Task<ActionResult<GradeDto>> GetGrade(int id)
        {



            var grade = await _context.Grades
                .Include(g => g.HomeroomTeacher)
                .Include(g => g.Students)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grade == null)
                return NotFound();

            return Ok(new GradeDto
            {
                Id = grade.Id,
                Name = grade.Name,
                Stream = grade.Stream,
                FullName = grade.FullName,
                Level = grade.Level,
                Section = grade.Section.ToString(),
                HomeroomTeacherId = grade.HomeroomTeacherId,
                HomeroomTeacherName = grade.HomeroomTeacher?.FullName,
                IsActive = grade.IsActive,
                StudentCount = grade.Students.Count(s => !s.IsArchived)
            });
        }

        /// <summary>
        /// Create new grade (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<GradeDto>> CreateGrade([FromBody] CreateGradeDto createGradeDto)
        {
            var grade = new Grade
            {
                Name = createGradeDto.Name,
                Stream = createGradeDto.Stream,
                Level = createGradeDto.Level,
                Section = createGradeDto.Section,
                HomeroomTeacherId = createGradeDto.HomeroomTeacherId ?? throw new ArgumentNullException(nameof(createGradeDto.HomeroomTeacherId))
            };

            _context.Grades.Add(grade);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGrade), new { id = grade.Id }, new GradeDto
            {
                Id = grade.Id,
                Name = grade.Name,
                Stream = grade.Stream,
                FullName = grade.FullName,
                Level = grade.Level,
                Section = grade.Section.ToString(),
                HomeroomTeacherId = grade.HomeroomTeacherId,
                IsActive = grade.IsActive,
                StudentCount = 0
            });
        }

        /// <summary>
        /// Assign homeroom teacher to grade (Admin only)
        /// </summary>
        [HttpPost("{id}/assign-homeroom-teacher")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AssignHomeroomTeacher(int id, [FromBody] AssignHomeroomTeacherDto assignDto)
        {
            var grade = await _context.Grades.FindAsync(id);
            if (grade == null)
                return NotFound();

            grade.HomeroomTeacherId = assignDto.TeacherId;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Homeroom teacher assigned successfully" });
        }


        /// Update grade (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<GradeDto>> UpdateGrade(int id, [FromBody] UpdateGradeDto updateGradeDto)
        {
            var grade = await _context.Grades
                .Include(g => g.HomeroomTeacher)
                .Include(g => g.Students)
                .FirstOrDefaultAsync(g => g.Id == id);
                
            if (grade == null)
                return NotFound();
        
            grade.Name = updateGradeDto.Name;
            grade.Stream = updateGradeDto.Stream;
            grade.Level = updateGradeDto.Level;
            grade.Section = updateGradeDto.Section;
            grade.HomeroomTeacherId = updateGradeDto.HomeroomTeacherId;
        
            await _context.SaveChangesAsync();
        
            return Ok(new GradeDto
            {
                Id = grade.Id,
                Name = grade.Name,
                Stream = grade.Stream,
                FullName = grade.FullName,
                Level = grade.Level,
                Section = grade.Section.ToString(),
                HomeroomTeacherId = grade.HomeroomTeacherId,
                HomeroomTeacherName = grade.HomeroomTeacher?.FullName,
                IsActive = grade.IsActive,
                StudentCount = grade.Students.Count(s => !s.IsArchived)
            });
        }
        /// <summary>
        /// Toggle grade status (Admin only)
        /// </summary>
        [HttpPatch("{id}/toggle-status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ToggleGradeStatus(int id)
        {
            var grade = await _context.Grades.FindAsync(id);
            if (grade == null)
                return NotFound();

            grade.IsActive = !grade.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Grade status updated successfully", isActive = grade.IsActive });
        }



    }
}