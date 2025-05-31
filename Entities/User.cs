using System.ComponentModel.DataAnnotations;
using BluebirdCore.Entities;

namespace BluebirdCore.Entities
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public required string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(100)]
        public required string FullName { get; set; }

        [Required]
        [StringLength(100)]
        public required string Email { get; set; }

        [Required]
        public UserRole Role { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        public virtual ICollection<TeacherSubjectAssignment> TeacherAssignments { get; set; } = new List<TeacherSubjectAssignment>();
        public virtual ICollection<Grade> HomeroomGrades { get; set; } = new List<Grade>();

    }
    
     public enum UserRole
    {
        Admin = 1,
        Teacher = 2,
        Staff = 3
    }
}