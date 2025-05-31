using BluebirdCore.Entities;

namespace BluebirdCore.Entities
{
    public class StudentOptionalSubject
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int SubjectId { get; set; }
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Student Student { get; set; }
        public virtual Subject Subject { get; set; }
    }
}