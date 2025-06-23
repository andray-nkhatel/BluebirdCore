using BluebirdCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace BluebirdCore.Data
{
    public class SchoolDbContext : DbContext
    {
        public SchoolDbContext(DbContextOptions<SchoolDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<GradeSubject> GradeSubjects { get; set; }
        public DbSet<StudentOptionalSubject> StudentOptionalSubjects { get; set; }
        public DbSet<TeacherSubjectAssignment> TeacherSubjectAssignments { get; set; }
        public DbSet<ExamType> ExamTypes { get; set; }
        public DbSet<ExamScore> ExamScores { get; set; }
        public DbSet<AcademicYear> AcademicYears { get; set; }
        public DbSet<ReportCard> ReportCards { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configurations
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Role).HasConversion<int>();
            });

            // Student configurations
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasIndex(e => e.StudentNumber).IsUnique();
                entity.HasOne(e => e.Grade)
                      .WithMany(e => e.Students)
                      .HasForeignKey(e => e.GradeId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Grade configurations
            modelBuilder.Entity<Grade>(entity =>
            {
                entity.Property(e => e.Section).HasConversion<int>();
                entity.HasOne(e => e.HomeroomTeacher)
                      .WithMany(e => e.HomeroomGrades)
                      .HasForeignKey(e => e.HomeroomTeacherId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // GradeSubject configurations
            modelBuilder.Entity<GradeSubject>(entity =>
            {
                entity.HasOne(e => e.Grade)
                      .WithMany(e => e.GradeSubjects)
                      .HasForeignKey(e => e.GradeId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Subject)
                      .WithMany(e => e.GradeSubjects)
                      .HasForeignKey(e => e.SubjectId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasIndex(e => new { e.GradeId, e.SubjectId }).IsUnique();
            });

            // StudentOptionalSubject configurations
            modelBuilder.Entity<StudentOptionalSubject>(entity =>
            {
                entity.HasOne(e => e.Student)
                      .WithMany(e => e.OptionalSubjects)
                      .HasForeignKey(e => e.StudentId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Subject)
                      .WithMany(e => e.StudentOptionalSubjects)
                      .HasForeignKey(e => e.SubjectId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasIndex(e => new { e.StudentId, e.SubjectId }).IsUnique();
            });

            // TeacherSubjectAssignment configurations
            modelBuilder.Entity<TeacherSubjectAssignment>(entity =>
            {
                entity.HasOne(e => e.Teacher)
                      .WithMany(e => e.TeacherAssignments)
                      .HasForeignKey(e => e.TeacherId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Subject)
                      .WithMany(e => e.TeacherAssignments)
                      .HasForeignKey(e => e.SubjectId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Grade)
                      .WithMany()
                      .HasForeignKey(e => e.GradeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ExamScore configurations
            modelBuilder.Entity<ExamScore>(entity =>
            {
                entity.Property(e => e.Score).HasPrecision(5, 2);
                
                entity.HasOne(e => e.Student)
                      .WithMany(e => e.ExamScores)
                      .HasForeignKey(e => e.StudentId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Subject)
                      .WithMany(e => e.ExamScores)
                      .HasForeignKey(e => e.SubjectId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.ExamType)
                      .WithMany(e => e.ExamScores)
                      .HasForeignKey(e => e.ExamTypeId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.Grade)
                      .WithMany()
                      .HasForeignKey(e => e.GradeId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.RecordedByTeacher)
                      .WithMany()
                      .HasForeignKey(e => e.RecordedBy)
                      .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasIndex(e => new { e.StudentId, e.SubjectId, e.ExamTypeId, e.AcademicYear, e.Term }).IsUnique();
            });

            // ReportCard configurations
            modelBuilder.Entity<ReportCard>(entity =>
            {
                entity.HasOne(e => e.Student)
                      .WithMany()
                      .HasForeignKey(e => e.StudentId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Grade)
                      .WithMany()
                      .HasForeignKey(e => e.GradeId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.GeneratedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.GeneratedBy)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed exam types
            modelBuilder.Entity<ExamType>().HasData(
                new ExamType { Id = 1, Name = "Test-One", Description = "First test of the term", Order = 1 },
                new ExamType { Id = 2, Name = "Test-Two", Description = "Second-test examination", Order = 2 },
                new ExamType { Id = 3, Name = "End-of-Term", Description = "End of term examination", Order = 3 }
            );

            // Seed academic year
            modelBuilder.Entity<AcademicYear>().HasData(
                new AcademicYear 
                { 
                    Id = 1, 
                    Name = "2025", 
                    StartDate = new DateTime(2025, 1, 1), 
                    EndDate = new DateTime(2025, 12, 1), 
                    IsActive = true 
                }
            );

            // Seed admin user (password: admin123)
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    PasswordHash = "$2a$12$Y5Cr10SW4OuJq6qxj7PXtOhZvb7loVQqIRRwcrH8hsdsoeRCririq",
                    FullName = "System Administrator",
                    Email = "admin@school.edu",
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // Seed some basic subjects
            modelBuilder.Entity<Subject>().HasData(
                new Subject { Id = 1, Name = "Mathematics", Code = "MATH" },
                new Subject { Id = 2, Name = "English", Code = "ENG" },
                new Subject { Id = 3, Name = "Science", Code = "SCI" },
                new Subject { Id = 4, Name = "Social Studies", Code = "SS" },
                new Subject { Id = 5, Name = "French", Code = "FR" },
                new Subject { Id = 6, Name = "ICT", Code = "ICT" },
                new Subject { Id = 7, Name = "Physical Education", Code = "PE" },
                new Subject { Id = 8, Name = "Art", Code = "ART" }
            );

            // Seed some grades
            modelBuilder.Entity<Grade>().HasData(
               

                 // PRESCHOOL SECTION (Levels 0-8) - Permanent for both systems
                new Grade { Id = 1, Name = "Baby-Class", Stream = "Purple", Level = 0, Section = SchoolSection.Preschool, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 2, Name = "Baby-Class", Stream = "Green", Level = 1, Section = SchoolSection.Preschool, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 3, Name = "Baby-Class", Stream = "Orange", Level = 2, Section = SchoolSection.Preschool, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 4, Name = "Middle-Class", Stream = "Purple", Level = 3, Section = SchoolSection.Preschool, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 5, Name = "Middle-Class", Stream = "Green", Level = 4, Section = SchoolSection.Preschool, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 6, Name = "Middle-Class", Stream = "Orange", Level = 5, Section = SchoolSection.Preschool, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 7, Name = "Reception-Class", Stream = "Purple", Level = 6, Section = SchoolSection.Preschool, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 8, Name = "Reception-Class", Stream = "Green", Level = 7, Section = SchoolSection.Preschool, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 9, Name = "Reception-Class", Stream = "Orange", Level = 8, Section = SchoolSection.Preschool, CurriculumType = CurriculumType.Legacy },

                // PRIMARY SECTION - LEGACY SYSTEM (Levels 9-29)
                // Grade 1 - Permanent
                new Grade { Id = 10, Name = "Grade 1", Stream = "Purple", Level = 9, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 11, Name = "Grade 1", Stream = "Green", Level = 10, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 12, Name = "Grade 1", Stream = "Orange", Level = 11, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy },
                // Grade 2 - Permanent
                new Grade { Id = 13, Name = "Grade 2", Stream = "Purple", Level = 12, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 14, Name = "Grade 2", Stream = "Green", Level = 13, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 15, Name = "Grade 2", Stream = "Orange", Level = 14, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy },
                // Grade 3 - Permanent
                new Grade { Id = 16, Name = "Grade 3", Stream = "Purple", Level = 15, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 17, Name = "Grade 3", Stream = "Green", Level = 16, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 18, Name = "Grade 3", Stream = "Orange", Level = 17, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy },
                // Grade 4 - Permanent
                new Grade { Id = 19, Name = "Grade 4", Stream = "Purple", Level = 18, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 20, Name = "Grade 4", Stream = "Green", Level = 19, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 21, Name = "Grade 4", Stream = "Orange", Level = 20, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy },
                // Grade 5 - Permanent
                new Grade { Id = 22, Name = "Grade 5", Stream = "Purple", Level = 21, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 23, Name = "Grade 5", Stream = "Green", Level = 22, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 24, Name = "Grade 5", Stream = "Orange", Level = 23, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy },
                // Grade 6 - Permanent
                new Grade { Id = 25, Name = "Grade 6", Stream = "Purple", Level = 24, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 26, Name = "Grade 6", Stream = "Green", Level = 25, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 27, Name = "Grade 6", Stream = "Orange", Level = 26, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy },
                
                // Grade 7 - TRANSITIONAL (Will be phased out by 2028)
                new Grade { Id = 28, Name = "Grade 7", Stream = "Purple", Level = 27, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy, IsTransitional = true, PhaseOutYear = 2028, ValidForCohorts = "2025,2026,2027,2028" },
                new Grade { Id = 29, Name = "Grade 7", Stream = "Green", Level = 28, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy, IsTransitional = true, PhaseOutYear = 2028, ValidForCohorts = "2025,2026,2027,2028" },
                new Grade { Id = 30, Name = "Grade 7", Stream = "Orange", Level = 29, Section = SchoolSection.Primary, CurriculumType = CurriculumType.Legacy, IsTransitional = true, PhaseOutYear = 2028, ValidForCohorts = "2025,2026,2027,2028"},

                // SECONDARY SECTION - LEGACY SYSTEM (Grades 8-12, Levels 30-39) - Permanent
                new Grade { Id = 31, Name = "Grade 8", Stream = "Grey", Level = 30, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 32, Name = "Grade 8", Stream = "Blue", Level = 31, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 33, Name = "Grade 9", Stream = "Grey", Level = 32, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 34, Name = "Grade 9", Stream = "Blue", Level = 33, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 35, Name = "Grade 10", Stream = "Grey", Level = 34, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 36, Name = "Grade 10", Stream = "Blue", Level = 35, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 37, Name = "Grade 11", Stream = "Grey", Level = 36, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 38, Name = "Grade 11", Stream = "Blue", Level = 37, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 39, Name = "Grade 12", Stream = "Grey", Level = 38, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.Legacy },
                new Grade { Id = 40, Name = "Grade 12", Stream = "Blue", Level = 39, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.Legacy },

                // CompetencyBased SYSTEM - SECONDARY SECTION (Forms 1-6) - Introduced 2024
                // Form 1 - Starts 2024 (for students who were in Grade 7 in 2024) - Levels 27-29
                new Grade { Id = 41, Name = "Form 1", Stream = "Grey", Level = 27, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.CompetencyBased, IntroducedYear = 2025 },
                new Grade { Id = 42, Name = "Form 1", Stream = "Blue", Level = 28, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.CompetencyBased, IntroducedYear = 2025 },
               
                
                // Form 2 - Starts 2025 - Levels 30-32
                new Grade { Id = 43, Name = "Form 2", Stream = "Grey", Level = 30, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.CompetencyBased, IntroducedYear = 2026},
                new Grade { Id = 44, Name = "Form 2", Stream = "Blue", Level = 31, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.CompetencyBased, IntroducedYear = 2026 },
               
                
                // Form 3 - Starts 2026 - Levels 33-35
                new Grade { Id = 45, Name = "Form 3", Stream = "Grey", Level = 33, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.CompetencyBased, IntroducedYear = 2027 },
                new Grade { Id = 46, Name = "Form 3", Stream = "Blue", Level = 34, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.CompetencyBased, IntroducedYear = 2027 },
              
                
                // Form 4 - Starts 2027 - Levels 36-38
                new Grade { Id = 47, Name = "Form 4", Stream = "Grey", Level = 36, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.CompetencyBased, IntroducedYear = 2028 },
                new Grade { Id = 48, Name = "Form 4", Stream = "Blue", Level = 37, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.CompetencyBased, IntroducedYear = 2028 },
               
                
                // Form 5 - Starts 2028 - Levels 39-41
                new Grade { Id = 49, Name = "Form 5", Stream = "Grey", Level = 39, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.CompetencyBased, IntroducedYear = 2029 },
                new Grade { Id = 50, Name = "Form 5", Stream = "Blue", Level = 40, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.CompetencyBased, IntroducedYear = 2029 },

                // Form 5 - Starts 2028 - Levels 39-41
                new Grade { Id = 51, Name = "Form 6", Stream = "Grey", Level = 39, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.CompetencyBased, IntroducedYear = 2030 },
                new Grade { Id = 52, Name = "Form 6", Stream = "Blue", Level = 40, Section = SchoolSection.Secondary, CurriculumType = CurriculumType.CompetencyBased, IntroducedYear = 2030 }
               
                
          
            );
        }
    }
}