namespace BluebirdCore.Entities
{
    public class GradeSubject
    {
        public int Id { get; set; }
        public int GradeId { get; set; }
        public int SubjectId { get; set; }
        public bool IsOptional { get; set; } = false;
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual Grade Grade { get; set; }
        public virtual Subject Subject { get; set; }
    }
}