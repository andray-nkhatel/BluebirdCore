using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BluebirdCore.Entities
{
    public class Grade
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public required string Name { get; set; } // e.g., "Grade 1", "Form 1"

        [Required]
        [StringLength(50)]
        public required string Stream { get; set; } // e.g., "Blue", "Grey", etc.

        public int Level { get; set; } 

        public SchoolSection Section { get; set; }

        public int? HomeroomTeacherId { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual User HomeroomTeacher { get; set; }

        public virtual ICollection<Student> Students { get; set; } = new List<Student>();

        public virtual ICollection<GradeSubject> GradeSubjects { get; set; } = new List<GradeSubject>();

        [NotMapped]
        public string FullName => string.IsNullOrEmpty(Stream) ? Name : $"{Name} {Stream}";

       

    }

    public enum SchoolSection
    {
        Preschool = 0,
        Primary = 1,
        Secondary = 2
    }

 
}