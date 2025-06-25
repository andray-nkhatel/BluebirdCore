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
        private readonly ILogger<AcademicYearsController> _logger;

        public AcademicYearsController(IAcademicYearService academicYearService, ILogger<AcademicYearsController> logger)
        {
            _academicYearService = academicYearService;
            _logger = logger;
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
        /// Get academic year by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Teacher,Staff")]
        public async Task<ActionResult<AcademicYear>> GetAcademicYear(int id)
        {
            var academicYear = await _academicYearService.GetAcademicYearByIdAsync(id);
            if (academicYear == null)
                return NotFound();

            return Ok(academicYear);
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
            try
            {
                if (createAcademicYearDto == null)
                    return BadRequest("Academic year data is required");

                var academicYear = new AcademicYear
                {
                    Name = createAcademicYearDto.Name,
                    StartDate = createAcademicYearDto.StartDate,
                    EndDate = createAcademicYearDto.EndDate
                };

                var createdYear = await _academicYearService.CreateAcademicYearAsync(academicYear);
                return CreatedAtAction(nameof(GetAcademicYear), new { id = createdYear.Id }, createdYear);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating academic year");
                return StatusCode(500, "An error occurred while creating the academic year");
            }
        }

        /// <summary>
        /// Update academic year (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<AcademicYear>> UpdateAcademicYear(int id, [FromBody] UpdateAcademicYearDto updateAcademicYearDto)
        {
            try
            {
                if (updateAcademicYearDto == null)
                    return BadRequest("Academic year data is required");

                var existingAcademicYear = await _academicYearService.GetAcademicYearByIdAsync(id);
                if (existingAcademicYear == null)
                    return NotFound();

                // Update properties
                existingAcademicYear.Name = updateAcademicYearDto.Name;
                existingAcademicYear.StartDate = updateAcademicYearDto.StartDate;
                existingAcademicYear.EndDate = updateAcademicYearDto.EndDate;

                var updatedYear = await _academicYearService.UpdateAcademicYearAsync(existingAcademicYear);
                return Ok(updatedYear);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating academic year with ID {Id}", id);
                return StatusCode(500, "An error occurred while updating the academic year");
            }
        }

        /// <summary>
        /// Delete academic year (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteAcademicYear(int id)
        {
            try
            {
                var existingAcademicYear = await _academicYearService.GetAcademicYearByIdAsync(id);
                if (existingAcademicYear == null)
                    return NotFound();

                var success = await _academicYearService.DeleteAcademicYearAsync(id);
                if (!success)
                    return BadRequest("Failed to delete academic year");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting academic year with ID {Id}", id);
                return StatusCode(500, "An error occurred while deleting the academic year");
            }
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
        /// Promote all students to next grade with curriculum transition support (Admin only)
        /// </summary>
        [HttpPost("{academicYearId}/promote-all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PromoteAllStudents(int academicYearId)
        {
            try
            {
                var result = await _academicYearService.PromoteStudentsPreservingStreamAsync();

                return Ok();
               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk promotion for Academic Year {AcademicYearId}", academicYearId);
                return StatusCode(500, new 
                { 
                    message = "An unexpected error occurred during student promotion",
                    error = ex.Message
                });
            }
        }

     
        // /// <summary>
        /// Archive graduates from both Grade 12 and Form 6 (Admin only)
        /// </summary>
        // [HttpPost("{academicYearId}/archive-graduates")]
        // [Authorize(Roles = "Admin")]
        // public async Task<ActionResult> ArchiveGraduates(int academicYearId)
        // {
        //     try
        //     {
        //         //var success = await _academicYearService.ArchiveGraduatesAsync(academicYearId);
        //         var success = await _academicYearService.ArchiveGraduatesAsync(academicYearId);
        //         if (!success)
        //             return BadRequest(new { message = "Failed to archive graduates or no graduates found" });

        //         return Ok(new { message = "Graduates archived successfully from both Grade 12 (Legacy) and Form 6 (Cambridge)" });
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error archiving graduates for Academic Year {AcademicYearId}", academicYearId);
        //         return StatusCode(500, new { message = "An error occurred while archiving graduates", error = ex.Message });
        //     }
        // }
    }

    // DTO for selective grade promotion
    public class PromoteSelectedGradesDto
    {
        public List<int> GradeIds { get; set; } = new();
    }
}