using BluebirdCore.DTOs;
using BluebirdCore.Entities;
using BluebirdCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BluebirdCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AcademicYearsController : ControllerBase
    {
        private readonly IAcademicYearService _academicYearService;

        public AcademicYearsController(IAcademicYearService academicYearService)
        {
            _academicYearService = academicYearService;
        }

        /// <summary>
        /// Get all academic years
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher,Staff")]
        public async Task<ActionResult<IEnumerable<AcademicYear>>> GetAcademicYears()
        {
            var academicYears = await _academicYearService.GetAllAcademicYearsAsync();
            return Ok(academicYears);
        }

        /// <summary>
        /// Get active academic year
        /// </summary>
        [HttpGet("active")]
        [Authorize(Roles = "Admin,Teacher,Staff")]
        public async Task<ActionResult<AcademicYear>> GetActiveAcademicYear()
        {
            var activeYear = await _academicYearService.GetActiveAcademicYearAsync();
            if (activeYear == null)
                return NotFound();

            return Ok(activeYear);
        }

        /// <summary>
        /// Create new academic year (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<AcademicYear>> CreateAcademicYear([FromBody] CreateAcademicYearDto createAcademicYearDto)
        {
            var academicYear = new AcademicYear
            {
                Name = createAcademicYearDto.Name,
                StartDate = createAcademicYearDto.StartDate,
                EndDate = createAcademicYearDto.EndDate
            };

            var createdYear = await _academicYearService.CreateAcademicYearAsync(academicYear);
            return CreatedAtAction(nameof(GetActiveAcademicYear), createdYear);
        }

        /// <summary>
        /// Close academic year and promote students (Admin only)
        /// </summary>
        [HttpPost("{academicYearId}/close")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> CloseAcademicYear(int academicYearId)
        {
            var success = await _academicYearService.CloseAcademicYearAsync(academicYearId);
            if (!success)
                return NotFound();

            return Ok(new { message = "Academic year closed successfully" });
        }

        /// <summary>
        /// Promote all students to next grade (Admin only)
        /// </summary>
        [HttpPost("{academicYearId}/promote-all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PromoteAllStudents(int academicYearId)
        {
            var success = await _academicYearService.PromoteAllStudentsAsync(academicYearId);
            if (!success)
                return BadRequest(new { message = "Failed to promote students" });

            return Ok(new { message = "All students promoted successfully" });
        }

        /// <summary>
        /// Archive graduates (Admin only)
        /// </summary>
        [HttpPost("{academicYearId}/archive-graduates")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ArchiveGraduates(int academicYearId)
        {
            var success = await _academicYearService.ArchiveGraduatesAsync(academicYearId);
            if (!success)
                return BadRequest(new { message = "Failed to archive graduates" });

            return Ok(new { message = "Graduates archived successfully" });
        }
    }
}
