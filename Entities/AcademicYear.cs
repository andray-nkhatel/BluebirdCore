using System.ComponentModel.DataAnnotations;

namespace BluebirdCore.Entities
{
    public class AcademicYear
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(20)]
        public required string Name { get; set; } // e.g., "2024-2025"
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsClosed { get; set; } = false;
    }
}