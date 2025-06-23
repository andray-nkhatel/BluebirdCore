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

        public int Level { get; set; } // For ordering/position in progression (e.g., 0–47)

        public SchoolSection Section { get; set; }

        public int? HomeroomTeacherId { get; set; }

        public bool IsActive { get; set; } = true;

        // 🆕 Curriculum type: Legacy or Cambridge
        public CurriculumType CurriculumType { get; set; }

        // 🆕 For transition tracking
        public bool IsTransitional { get; set; } = false;

        public int? PhaseOutYear { get; set; } // e.g., 2028

        public int? IntroducedYear { get; set; } // e.g., 2025

        // 🆕 For specifying allowed student cohorts, e.g. "2022,2023,2024"
        [StringLength(100)]
        public string? ValidForCohorts { get; set; }

        // Navigation properties
        public virtual User HomeroomTeacher { get; set; }

        public virtual ICollection<Student> Students { get; set; } = new List<Student>();

        public virtual ICollection<GradeSubject> GradeSubjects { get; set; } = new List<GradeSubject>();

        [NotMapped]
        public string FullName => string.IsNullOrEmpty(Stream) ? Name : $"{Name} {Stream}";

        // 🆕 Helper Methods for Transition Management
        
        /// <summary>
        /// Checks if this grade is valid/available for a specific academic year
        /// </summary>
        /// <param name="academicYear">The academic year to check (e.g., 2024)</param>
        /// <returns>True if the grade is available for that year</returns>
        public bool IsValidForYear(int academicYear)
        {
            if (!IsActive) return false;
            
            // Check if grade hasn't been introduced yet
            if (IntroducedYear.HasValue && academicYear < IntroducedYear.Value)
                return false;
                
            // Check if grade has been phased out
            if (PhaseOutYear.HasValue && academicYear > PhaseOutYear.Value)
                return false;
                
            return true;
        }
        
        /// <summary>
        /// Checks if this grade is valid for a specific student cohort year
        /// </summary>
        /// <param name="cohortYear">The year the student cohort started Grade 1 (e.g., 2022)</param>
        /// <returns>True if the grade allows this cohort</returns>
        public bool IsValidForCohort(int cohortYear)
        {
            // If no cohort restrictions, allow all cohorts
            if (string.IsNullOrEmpty(ValidForCohorts)) 
                return true;
            
            // Parse the comma-separated cohort years
            var validYears = ValidForCohorts.Split(',')
                .Select(y => int.TryParse(y.Trim(), out int year) ? year : (int?)null)
                .Where(y => y.HasValue)
                .Select(y => y.Value);
                
            return validYears.Contains(cohortYear);
        }

        /// <summary>
        /// Gets a description of the transition status for this grade
        /// </summary>
        /// <returns>A string describing the transition status</returns>
        [NotMapped]
        public string TransitionStatus
        {
            get
            {
                if (!IsTransitional)
                    return "Permanent";
                
                var status = "Transitional";
                
                if (PhaseOutYear.HasValue)
                    status += $" (phases out {PhaseOutYear})";
                    
                if (IntroducedYear.HasValue)
                    status += $" (introduced {IntroducedYear})";
                    
                if (!string.IsNullOrEmpty(ValidForCohorts))
                    status += $" (cohorts: {ValidForCohorts})";
                    
                return status;
            }
        }

        /// <summary>
        /// Checks if this grade will be available in a future year
        /// </summary>
        /// <param name="futureYear">The future year to check</param>
        /// <returns>True if the grade will be available</returns>
        public bool WillBeAvailableInYear(int futureYear)
        {
            // Check if grade will be introduced by then
            if (IntroducedYear.HasValue && futureYear < IntroducedYear.Value)
                return false;
                
            // Check if grade will be phased out by then
            if (PhaseOutYear.HasValue && futureYear > PhaseOutYear.Value)
                return false;
                
            return true;
        }
    }

    public enum SchoolSection
    {
        Preschool = 0,
        Primary = 1,
        Secondary = 2
    }

    // 🆕 Add CurriculumType enum if not already present
    public enum CurriculumType
    {
        Legacy = 0,
        CompetencyBased = 1
    }
}