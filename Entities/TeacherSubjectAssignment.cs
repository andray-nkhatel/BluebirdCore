using BluebirdCore.Entities;

namespace BluebirdCore.Entities
{
    public class TeacherSubjectAssignment
    {
        public int Id { get; set; }
        public int TeacherId { get; set; }
        public int SubjectId { get; set; }
        public int GradeId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User Teacher { get; set; }
        public virtual Subject Subject { get; set; }
        public virtual Grade Grade { get; set; }
    }
}