using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BluebirdCore.Entities
{
    public class Student
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public required string FirstName { get; set; }
        
        [Required]
        [StringLength(50)]
        public required string LastName { get; set; }
        
        [StringLength(50)]
        public string? MiddleName { get; set; }
        
        [Required]
        [StringLength(20)]
        public string? StudentNumber { get; set; }
        
        public DateTime DateOfBirth { get; set; }
        
        [StringLength(10)]
        public string? Gender { get; set; }
        
        [StringLength(200)]
        public string? Address { get; set; }
        
        [StringLength(15)]
        public string? PhoneNumber { get; set; }
        
        [StringLength(100)]
        public string? GuardianName { get; set; }
        
        [StringLength(15)]
        public string? GuardianPhone { get; set; }
        
        public int GradeId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsArchived { get; set; } = false;
        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
        public DateTime ArchiveDate { get; set; }
        
        // Navigation properties
        public virtual Grade Grade { get; set; }
        public virtual ICollection<ExamScore>? ExamScores { get; set; } = new List<ExamScore>();
        public virtual ICollection<StudentOptionalSubject>? OptionalSubjects { get; set; } = new List<StudentOptionalSubject>();
        [NotMapped]
        public string FullName => $"{FirstName} {MiddleName} {LastName}".Replace("  ", " ").Trim();
    }
}