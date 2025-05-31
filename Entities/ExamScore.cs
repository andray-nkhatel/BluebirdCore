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
        
        // Navigation properties
        public virtual Student Student { get; set; }
        public virtual Subject Subject { get; set; }
        public virtual ExamType ExamType { get; set; }
        public virtual Grade Grade { get; set; }
        public virtual User RecordedByTeacher { get; set; }
    }

}