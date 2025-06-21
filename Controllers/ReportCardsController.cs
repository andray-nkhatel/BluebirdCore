using BluebirdCore.DTOs;
using BluebirdCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BluebirdCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportCardsController : ControllerBase
    {
        private readonly IReportCardService _reportCardService;

        public ReportCardsController(IReportCardService reportCardService)
        {
            _reportCardService = reportCardService;
        }

        /// <summary>
        /// Generate report card for a student (Admin only)
        /// </summary>
        [HttpPost("generate/student/{studentId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ReportCardDto>> GenerateReportCard(int studentId, [FromQuery] int academicYear, [FromQuery] int term)
        {
            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            try
            {
                var reportCard = await _reportCardService.GenerateReportCardAsync(studentId, academicYear, term, adminId);

                return Ok(new ReportCardDto
                {
                    Id = reportCard.Id,
                    StudentId = reportCard.StudentId,
                    StudentName = reportCard.Student?.FullName ?? "",
                    GradeName = reportCard.Grade?.FullName ?? "",
                    AcademicYear = reportCard.AcademicYear,
                    Term = reportCard.Term,
                    GeneratedAt = reportCard.GeneratedAt,
                    GeneratedByName = reportCard.GeneratedByUser?.FullName ?? ""
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Generate report cards for entire class (Admin only)
        /// </summary>
        [HttpPost("generate/class/{gradeId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ReportCardDto>>> GenerateClassReportCards(int gradeId, [FromQuery] int academicYear, [FromQuery] int term)
        {
            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            try
            {
                var reportCards = await _reportCardService.GenerateClassReportCardsAsync(gradeId, academicYear, term, adminId);

                var reportCardDtos = reportCards.Select(rc => new ReportCardDto
                {
                    Id = rc.Id,
                    StudentId = rc.StudentId,
                    StudentName = rc.Student?.FullName ?? "",
                    GradeName = rc.Grade?.FullName ?? "",
                    AcademicYear = rc.AcademicYear,
                    Term = rc.Term,
                    GeneratedAt = rc.GeneratedAt,
                    GeneratedByName = rc.GeneratedByUser?.FullName ?? ""
                });

                return Ok(reportCardDtos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Download report card PDF (Admin only)
        /// </summary>
        [HttpGet("{reportCardId}/download")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DownloadReportCard(int reportCardId)
        {
            var pdfBytes = await _reportCardService.GetReportCardPdfAsync(reportCardId);
            if (pdfBytes == null || pdfBytes.Length == 0)
                return NotFound(new { message = "PDF not found for this report card." });

            return File(pdfBytes, "application/pdf", $"ReportCard_{reportCardId}.pdf");
        }

        /// <summary>
        /// Get all report cards for a student
        /// </summary>
        [HttpGet("student/{studentId}")]
        [Authorize(Roles = "Admin,Teacher,Staff")]
        public async Task<ActionResult<IEnumerable<ReportCardDto>>> GetStudentReportCards(int studentId)
        {
            var reportCards = await _reportCardService.GetStudentReportCardsAsync(studentId);

            var reportCardDtos = reportCards.Select(rc => new ReportCardDto
            {
                Id = rc.Id,
                StudentId = rc.StudentId,
                StudentName = rc.Student?.FullName ?? "",
                GradeName = rc.Grade?.FullName ?? "",
                AcademicYear = rc.AcademicYear,
                Term = rc.Term,
                GeneratedAt = rc.GeneratedAt,
                GeneratedByName = rc.GeneratedByUser?.FullName ?? ""
            });

            return Ok(reportCardDtos);
        }


        /// <summary>
        /// Delete ALL report cards (Admin only, irreversible!)
        /// </summary>
        [HttpDelete("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAllReportCards()
        {
            await _reportCardService.DeleteAllReportCardsAsync();
            return NoContent();
        }
    }
}