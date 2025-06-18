using System.ComponentModel.DataAnnotations;
using BluebirdCore.Entities;

namespace BluebirdCore.Entities
{
    public class ExamScore
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int SubjectId { get; set; }
        public int ExamTypeId { get; set; }
        public int GradeId { get; set; }
        
        [Range(0, 100)]
        public decimal Score { get; set; }
        
        public int AcademicYear { get; set; }
        public int Term { get; set; } // 1, 2, 3
        
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        public int RecordedBy { get; set; } // Teacher who entered the score
        
        // Comment fields
        [StringLength(1000)] // Adjust max length as needed
        public string? Comments { get; set; }
        public DateTime? CommentsUpdatedAt { get; set; }
        public int? CommentsUpdatedBy { get; set; } // Track who last updated comments
        
        // Navigation properties
        public virtual Student Student { get; set; }
        public virtual Subject Subject { get; set; }
        public virtual ExamType ExamType { get; set; }
        public virtual Grade Grade { get; set; }
        public virtual User RecordedByTeacher { get; set; }
        public virtual User? CommentsUpdatedByTeacher { get; set; }
    }
}