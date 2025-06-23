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
        public async Task<ActionResult<IEnumerable<SubjectDto>>> GetSubjects([FromQuery] bool includeInactive = false)
        {
            var query = _context.Subjects.AsQueryable();

            // Only filter by IsActive if includeInactive is false
            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            var subjects = await query
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
        /// Update subject (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SubjectDto>> UpdateSubject(int id, [FromBody] UpdateSubjectDto updateSubjectDto)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return NotFound();

            subject.Name = updateSubjectDto.Name;
            subject.Code = updateSubjectDto.Code;
            subject.Description = updateSubjectDto.Description;
            subject.IsActive = updateSubjectDto.IsActive;

            await _context.SaveChangesAsync();

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
        /// Toggle subject active status (Admin only)
        /// </summary>
        [HttpPatch("{id}/toggle")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SubjectDto>> ToggleSubjectStatus(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return NotFound(new { message = "Subject not found" });

            // Toggle the IsActive status
            subject.IsActive = !subject.IsActive;

            // Update timestamp if your entity has one
            // subject.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

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
        /// Assign subject to grade (Admin only)
        /// </summary>
        [HttpPost("{subjectId}/assign-to-grade/{gradeId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AssignSubjectToGrade(int subjectId, int gradeId, [FromBody] AssignSubjectToGradeDto assignDto)
        {
            var subject = await _context.Subjects.FindAsync(subjectId);
            var grade = await _context.Grades.FindAsync(gradeId);

            if (subject == null) return NotFound("Subject not found");
            if (grade == null) return NotFound("Grade not found");

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



        // Controller method for true bulk assignments across multiple grades
        [HttpPost("bulk-assign")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> BulkAssignTeachersToSubjects([FromBody] BulkAssignmentDto bulkDto)
        {
            var assignments = new List<TeacherSubjectAssignment>();
            var errors = new List<string>();
            var successful = 0;

            foreach (var assignment in bulkDto.Assignments)
            {
                try
                {
                    // Validate that entities exist
                    var teacher = await _context.Users.FindAsync(assignment.TeacherId);
                    var subject = await _context.Subjects.FindAsync(assignment.SubjectId);
                    var grade = await _context.Grades.FindAsync(assignment.GradeId);

                    if (teacher == null)
                    {
                        errors.Add($"Teacher with ID {assignment.TeacherId} not found");
                        continue;
                    }

                    if (subject == null)
                    {
                        errors.Add($"Subject with ID {assignment.SubjectId} not found");
                        continue;
                    }

                    if (grade == null)
                    {
                        errors.Add($"Grade with ID {assignment.GradeId} not found");
                        continue;
                    }

                    // Check for existing assignment
                    var existing = await _context.TeacherSubjectAssignments
                        .AnyAsync(tsa => tsa.TeacherId == assignment.TeacherId
                                      && tsa.SubjectId == assignment.SubjectId
                                      && tsa.GradeId == assignment.GradeId
                                      && tsa.IsActive);

                    if (existing)
                    {
                        errors.Add($"Teacher {teacher.FullName} already assigned to {subject.Name} for {grade.Name}");
                        continue;
                    }

                    assignments.Add(new TeacherSubjectAssignment
                    {
                        TeacherId = assignment.TeacherId,
                        SubjectId = assignment.SubjectId,
                        GradeId = assignment.GradeId

                    });

                    successful++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Error processing assignment: {ex.Message}");
                }
            }

            if (assignments.Any())
            {
                _context.TeacherSubjectAssignments.AddRange(assignments);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                message = $"Bulk assignment completed",
                successful = successful,
                failed = errors.Count,
                totalRequested = bulkDto.Assignments.Count,
                errors = errors
            });
        }

        // Option 2: Multiple grades for one teacher-subject combination
        [HttpPost("{subjectId}/assign-teacher-multiple-grades")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AssignTeacherToSubjectMultipleGrades(
            int subjectId,
            [FromBody] AssignTeacherToSubjectMultipleGradesDto assignDto)
        {
            var subject = await _context.Subjects.FindAsync(subjectId);
            if (subject == null)
                return NotFound("Subject not found");

            var teacher = await _context.Users.FindAsync(assignDto.TeacherId);
            if (teacher == null)
                return NotFound("Teacher not found");

            var existingAssignments = await _context.TeacherSubjectAssignments
                .Where(tsa => tsa.TeacherId == assignDto.TeacherId
                           && tsa.SubjectId == subjectId
                           && assignDto.GradeIds.Contains(tsa.GradeId)
                           && tsa.IsActive)
                .Select(tsa => tsa.GradeId)
                .ToListAsync();

            if (existingAssignments.Any())
            {
                var conflictingGrades = await _context.Grades
                    .Where(g => existingAssignments.Contains(g.Id))
                    .Select(g => g.Name)
                    .ToListAsync();

                return BadRequest(new
                {
                    message = "Teacher already assigned to this subject for some grades",
                    conflictingGrades = conflictingGrades
                });
            }

            var newGradeIds = assignDto.GradeIds.Except(existingAssignments).ToList();
            var assignments = newGradeIds.Select(gradeId => new TeacherSubjectAssignment
            {
                TeacherId = assignDto.TeacherId,
                SubjectId = subjectId,
                GradeId = gradeId,
            }).ToList();

            _context.TeacherSubjectAssignments.AddRange(assignments);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Teacher assigned to {assignments.Count} grade(s) successfully",
                assignedGrades = assignments.Count,
                skippedGrades = existingAssignments.Count
            });
        }
    

    // Additional endpoints you should add to handle assignment changes

/// <summary>
/// Remove teacher assignment (Admin only)
/// </summary>
[HttpDelete("assignments/{assignmentId}")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult> RemoveTeacherAssignment(int assignmentId)
{
    try
    {
        var assignment = await _context.TeacherSubjectAssignments.FindAsync(assignmentId);
        if (assignment == null)
            return NotFound(new { message = "Assignment not found." });

        // Soft delete by setting IsActive to false
        assignment.IsActive = false;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Teacher assignment removed successfully." });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "An error occurred while removing the assignment." });
    }
}

/// <summary>
/// Remove all assignments for a teacher from a specific subject (Admin only)
/// </summary>
[HttpDelete("{subjectId}/remove-teacher/{teacherId}")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult> RemoveTeacherFromSubject(int subjectId, int teacherId)
{
    try
    {
        var assignments = await _context.TeacherSubjectAssignments
            .Where(tsa => tsa.TeacherId == teacherId && tsa.SubjectId == subjectId && tsa.IsActive)
            .ToListAsync();

        if (!assignments.Any())
            return NotFound(new { message = "No active assignments found for this teacher and subject." });

        // Soft delete all assignments
        foreach (var assignment in assignments)
        {
            assignment.IsActive = false;
        }

        await _context.SaveChangesAsync();

        return Ok(new { 
            message = $"Removed {assignments.Count} assignment(s) successfully.",
            removedAssignments = assignments.Count 
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "An error occurred while removing teacher assignments." });
    }
}

/// <summary>
/// Get all assignments for a specific teacher (Admin, Teacher, Staff)
/// </summary>
[HttpGet("assignments/teacher/{teacherId}")]
[Authorize(Roles = "Admin,Teacher,Staff")]
public async Task<ActionResult> GetTeacherAssignments(int teacherId, [FromQuery] bool includeInactive = false)
{
    try
    {
        var query = _context.TeacherSubjectAssignments
            .Include(tsa => tsa.Subject)
            .Include(tsa => tsa.Grade)
            .Include(tsa => tsa.Teacher)
            .Where(tsa => tsa.TeacherId == teacherId);

        if (!includeInactive)
        {
            query = query.Where(tsa => tsa.IsActive);
        }

        var assignments = await query.ToListAsync();

        var result = assignments.Select(tsa => new
        {
            AssignmentId = tsa.Id,
            TeacherName = tsa.Teacher.FullName,
            SubjectName = tsa.Subject.Name,
            SubjectCode = tsa.Subject.Code,
            GradeName = tsa.Grade.Name,
            IsActive = tsa.IsActive
        });

        return Ok(result);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "An error occurred while retrieving teacher assignments." });
    }
}

/// <summary>
/// Get all assignments for a specific subject (Admin, Teacher, Staff)
/// </summary>
[HttpGet("{subjectId}/assignments")]
[Authorize(Roles = "Admin,Teacher,Staff")]
public async Task<ActionResult> GetSubjectAssignments(int subjectId, [FromQuery] bool includeInactive = false)
{
    try
    {
        var subject = await _context.Subjects.FindAsync(subjectId);
        if (subject == null)
            return NotFound(new { message = "Subject not found." });

        var query = _context.TeacherSubjectAssignments
            .Include(tsa => tsa.Teacher)
            .Include(tsa => tsa.Grade)
            .Where(tsa => tsa.SubjectId == subjectId);

        if (!includeInactive)
        {
            query = query.Where(tsa => tsa.IsActive);
        }

        var assignments = await query.ToListAsync();

        var result = assignments.Select(tsa => new
        {
            AssignmentId = tsa.Id,
            TeacherId = tsa.TeacherId,
            TeacherName = tsa.Teacher.FullName,
            GradeId = tsa.GradeId,
            GradeName = tsa.Grade.Name,
            IsActive = tsa.IsActive
        });

        return Ok(new
        {
            SubjectName = subject.Name,
            SubjectCode = subject.Code,
            Assignments = result
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "An error occurred while retrieving subject assignments." });
    }
}

/// <summary>
/// Transfer teacher assignments from one teacher to another (Admin only)
/// </summary>
[HttpPost("transfer-assignments")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult> TransferTeacherAssignments([FromBody] TransferAssignmentsDto transferDto)
{
    try
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var fromTeacher = await _context.Users.FindAsync(transferDto.FromTeacherId);
        var toTeacher = await _context.Users.FindAsync(transferDto.ToTeacherId);

        if (fromTeacher == null)
            return NotFound(new { message = "Source teacher not found." });
        if (toTeacher == null)
            return NotFound(new { message = "Destination teacher not found." });

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var assignmentsToTransfer = await _context.TeacherSubjectAssignments
                .Where(tsa => tsa.TeacherId == transferDto.FromTeacherId && tsa.IsActive)
                .ToListAsync();

            if (!assignmentsToTransfer.Any())
            {
                return BadRequest(new { message = "No active assignments found for the source teacher." });
            }

            var transferred = 0;
            var conflicts = new List<string>();

            foreach (var assignment in assignmentsToTransfer)
            {
                // Check if destination teacher already has this assignment
                var existingAssignment = await _context.TeacherSubjectAssignments
                    .FirstOrDefaultAsync(tsa => tsa.TeacherId == transferDto.ToTeacherId 
                                             && tsa.SubjectId == assignment.SubjectId
                                             && tsa.GradeId == assignment.GradeId
                                             && tsa.IsActive);

                if (existingAssignment != null)
                {
                    var subject = await _context.Subjects.FindAsync(assignment.SubjectId);
                    var grade = await _context.Grades.FindAsync(assignment.GradeId);
                    conflicts.Add($"{subject.Name} - {grade.Name}");
                    continue;
                }

                // Deactivate old assignment
                assignment.IsActive = false;

                // Create new assignment
                var newAssignment = new TeacherSubjectAssignment
                {
                    TeacherId = transferDto.ToTeacherId,
                    SubjectId = assignment.SubjectId,
                    GradeId = assignment.GradeId,
                    IsActive = true
                };

                _context.TeacherSubjectAssignments.Add(newAssignment);
                transferred++;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Assignment transfer completed",
                transferred = transferred,
                conflicts = conflicts.Count,
                conflictDetails = conflicts
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "An error occurred during assignment transfer." });
    }
}

// Required DTO for transfer operation
public class TransferAssignmentsDto
{
    public int FromTeacherId { get; set; }
    public int ToTeacherId { get; set; }
}




    }
}