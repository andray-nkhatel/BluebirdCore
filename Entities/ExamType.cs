using System.ComponentModel.DataAnnotations;

namespace BluebirdCore.Entities
{
    public class ExamType
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public required string Name { get; set; } // Test-One, Mid-Term, End-of-Term
        
        [StringLength(200)]
        public string? Description { get; set; }
        
        public int Order { get; set; } // For sorting
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<ExamScore> ExamScores { get; set; }
    }
}