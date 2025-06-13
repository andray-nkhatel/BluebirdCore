using BluebirdCore.DTOs;
using BluebirdCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BluebirdCore.Entities
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentsController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        /// <summary>
        /// Get all students
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher,Staff")]
        public async Task<ActionResult<IEnumerable<StudentDto>>> GetStudents([FromQuery] bool includeArchived = false)
        {
            var students = await _studentService.GetAllStudentsAsync(includeArchived);
            var studentDtos = students.Select(s => new StudentDto
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                MiddleName = s.MiddleName,
                StudentNumber = s.StudentNumber,
                DateOfBirth = s.DateOfBirth,
                Gender = s.Gender,
                Address = s.Address,
                PhoneNumber = s.PhoneNumber,
                GuardianName = s.GuardianName,
                GuardianPhone = s.GuardianPhone,
                GradeId = s.GradeId,
                GradeName = s.Grade?.FullName,
                IsActive = s.IsActive,
                IsArchived = s.IsArchived,
                EnrollmentDate = s.EnrollmentDate,
                FullName = s.FullName,
                OptionalSubjects = s.OptionalSubjects?.Select(os => new SubjectDto
                {
                    Id = os.Subject.Id,
                    Name = os.Subject.Name,
                    Code = os.Subject.Code
                }).ToList() ?? new List<SubjectDto>()
            });

            return Ok(studentDtos);
        }

        /// <summary>
        /// Get student by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Teacher,Staff")]
        public async Task<ActionResult<StudentDto>> GetStudent(int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null)
                return NotFound();

            return Ok(new StudentDto
            {
                Id = student.Id,
                FirstName = student.FirstName,
                LastName = student.LastName,
                MiddleName = student.MiddleName,
                StudentNumber = student.StudentNumber,
                DateOfBirth = student.DateOfBirth,
                Gender = student.Gender,
                Address = student.Address,
                PhoneNumber = student.PhoneNumber,
                GuardianName = student.GuardianName,
                GuardianPhone = student.GuardianPhone,
                GradeId = student.GradeId,
                GradeName = student.Grade?.FullName,
                IsActive = student.IsActive,
                IsArchived = student.IsArchived,
                EnrollmentDate = student.EnrollmentDate,
                FullName = student.FullName,
                OptionalSubjects = student.OptionalSubjects?.Select(os => new SubjectDto
                {
                    Id = os.Subject.Id,
                    Name = os.Subject.Name,
                    Code = os.Subject.Code
                }).ToList() ?? new List<SubjectDto>()
            });
        }


        /// <summary>
        /// Update an existing student (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<StudentDto>> UpdateStudent(int id, [FromBody] UpdateStudentDto updateStudentDto)
        {
            // Check if student exists
            var existingStudent = await _studentService.GetStudentByIdAsync(id);
            if (existingStudent == null)
                return NotFound();

            // Map fields from DTO to entity
            existingStudent.FirstName = updateStudentDto.FirstName;
            existingStudent.LastName = updateStudentDto.LastName;
            existingStudent.MiddleName = updateStudentDto.MiddleName;
            existingStudent.StudentNumber = updateStudentDto.StudentNumber;
            existingStudent.DateOfBirth = updateStudentDto.DateOfBirth;
            existingStudent.Gender = updateStudentDto.Gender;
            existingStudent.Address = updateStudentDto.Address;
            existingStudent.PhoneNumber = updateStudentDto.PhoneNumber;
            existingStudent.GuardianName = updateStudentDto.GuardianName;
            existingStudent.GuardianPhone = updateStudentDto.GuardianPhone;
            existingStudent.GradeId = updateStudentDto.GradeId;
            existingStudent.IsActive = updateStudentDto.IsActive;
            // existingStudent.IsArchived = updateStudentDto.IsArchived;
            
            // Add any other fields as needed

            try
            {
                var updatedStudent = await _studentService.UpdateStudentAsync(existingStudent);

                return Ok(new StudentDto
                {
                    Id = updatedStudent.Id,
                    FirstName = updatedStudent.FirstName,
                    LastName = updatedStudent.LastName,
                    MiddleName = updatedStudent.MiddleName,
                    StudentNumber = updatedStudent.StudentNumber,
                    DateOfBirth = updatedStudent.DateOfBirth,
                    Gender = updatedStudent.Gender,
                    Address = updatedStudent.Address,
                    PhoneNumber = updatedStudent.PhoneNumber,
                    GuardianName = updatedStudent.GuardianName,
                    GuardianPhone = updatedStudent.GuardianPhone,
                    GradeId = updatedStudent.GradeId,
                    GradeName = updatedStudent.Grade?.FullName,
                    IsActive = updatedStudent.IsActive,
                    IsArchived = updatedStudent.IsArchived,
                    EnrollmentDate = updatedStudent.EnrollmentDate,
                    FullName = updatedStudent.FullName,
                    OptionalSubjects = updatedStudent.OptionalSubjects?.Select(os => new SubjectDto
                    {
                        Id = os.Subject.Id,
                        Name = os.Subject.Name,
                        Code = os.Subject.Code
                    }).ToList() ?? new List<SubjectDto>()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }




        /// <summary>
        /// Create new student (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<StudentDto>> CreateStudent([FromBody] CreateStudentDto createStudentDto)
        {
            try
            {
                var student = new Entities.Student
                {
                    FirstName = createStudentDto.FirstName,
                    LastName = createStudentDto.LastName,
                    MiddleName = createStudentDto.MiddleName,
                    StudentNumber = createStudentDto.StudentNumber,
                    DateOfBirth = createStudentDto.DateOfBirth,
                    Gender = createStudentDto.Gender,
                    Address = createStudentDto.Address,
                    PhoneNumber = createStudentDto.PhoneNumber,
                    GuardianName = createStudentDto.GuardianName,
                    GuardianPhone = createStudentDto.GuardianPhone,
                    GradeId = createStudentDto.GradeId
                };

                var createdStudent = await _studentService.CreateStudentAsync(student);

                return CreatedAtAction(nameof(GetStudent), new { id = createdStudent.Id }, new StudentDto
                {
                    Id = createdStudent.Id,
                    FirstName = createdStudent.FirstName,
                    LastName = createdStudent.LastName,
                    MiddleName = createdStudent.MiddleName,
                    StudentNumber = createdStudent.StudentNumber,
                    DateOfBirth = createdStudent.DateOfBirth,
                    Gender = createdStudent.Gender,
                    Address = createdStudent.Address,
                    PhoneNumber = createdStudent.PhoneNumber,
                    GuardianName = createdStudent.GuardianName,
                    GuardianPhone = createdStudent.GuardianPhone,
                    GradeId = createdStudent.GradeId,
                    IsActive = createdStudent.IsActive,
                    IsArchived = createdStudent.IsArchived,
                    EnrollmentDate = createdStudent.EnrollmentDate,
                    FullName = createdStudent.FullName,
                    OptionalSubjects = new List<SubjectDto>()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get students by grade
        /// </summary>
        [HttpGet("grade/{gradeId}")]
        [Authorize(Roles = "Admin,Teacher,Staff")]
        public async Task<ActionResult<IEnumerable<StudentDto>>> GetStudentsByGrade(int gradeId)
        {
            var students = await _studentService.GetStudentsByGradeAsync(gradeId);
            var studentDtos = students.Select(s => new StudentDto
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                MiddleName = s.MiddleName,
                StudentNumber = s.StudentNumber,
                DateOfBirth = s.DateOfBirth,
                Gender = s.Gender,
                GradeId = s.GradeId,
                GradeName = s.Grade?.FullName,
                IsActive = s.IsActive,
                IsArchived = s.IsArchived,
                FullName = s.FullName
            });

            return Ok(studentDtos);
        }

        /// <summary>
        /// Archive student (Admin only)
        /// </summary>
        [HttpPost("{id}/archive")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ArchiveStudent(int id)
        {
            var success = await _studentService.ArchiveStudentAsync(id);
            if (!success)
                return NotFound();

            return Ok(new { message = "Student archived successfully" });
        }

        /// <summary>
        /// Promote students from one grade to another (Admin only)
        /// </summary>
        [HttpPost("promote")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PromoteStudents([FromBody] PromoteStudentsDto promoteDto)
        {
            var success = await _studentService.PromoteStudentsAsync(promoteDto.FromGradeId, promoteDto.ToGradeId);
            if (!success)
                return BadRequest(new { message = "Failed to promote students" });

            return Ok(new { message = "Students promoted successfully" });
        }

        /// <summary>
        /// Bulk import students from CSV (Admin only)
        /// </summary>
        [HttpPost("import/csv")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ImportStudentsFromCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please select a valid CSV file" });

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    var students = await _studentService.ImportStudentsFromCsvAsync(stream);
                    return Ok(new { message = $"Successfully imported {students.Count()} students" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
