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
                    Email = "admin@chsschool.com",
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt =  new DateTime(2025, 06, 25)
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
                new Grade { Id = 1, Name = "Baby-Class", Stream = "Purple", Level = 1, Section = SchoolSection.Preschool },
                new Grade { Id = 2, Name = "Baby-Class", Stream = "Green", Level = 1, Section = SchoolSection.Preschool },
                new Grade { Id = 3, Name = "Baby-Class", Stream = "Orange", Level = 1, Section = SchoolSection.Preschool },
                new Grade { Id = 4, Name = "Middle-Class", Stream = "Purple", Level = 2, Section = SchoolSection.Preschool },
                new Grade { Id = 5, Name = "Middle-Class", Stream = "Green", Level = 2, Section = SchoolSection.Preschool },
                new Grade { Id = 6, Name = "Middle-Class", Stream = "Orange", Level = 2, Section = SchoolSection.Preschool },
                new Grade { Id = 7, Name = "Reception-Class", Stream = "Purple", Level = 3, Section = SchoolSection.Preschool }, 
                new Grade { Id = 8, Name = "Reception-Class", Stream = "Green", Level = 3, Section = SchoolSection.Preschool }, 
                new Grade { Id = 9, Name = "Reception-Class", Stream = "Orange", Level = 3, Section = SchoolSection.Preschool }, 

                // PRIMARY SECTION
                // Grade 1 - Permanent
                new Grade { Id = 10, Name = "Grade 1", Stream = "Purple", Level = 4, Section = SchoolSection.Primary,},
                new Grade { Id = 11, Name = "Grade 1", Stream = "Green", Level = 4, Section = SchoolSection.Primary,},
                new Grade { Id = 12, Name = "Grade 1", Stream = "Orange", Level = 4, Section = SchoolSection.Primary },
                // Grade 2 - Permanent
                new Grade { Id = 13, Name = "Grade 2", Stream = "Purple", Level = 5, Section = SchoolSection.Primary },
                new Grade { Id = 14, Name = "Grade 2", Stream = "Green", Level = 5, Section = SchoolSection.Primary,},
                new Grade { Id = 15, Name = "Grade 2", Stream = "Orange", Level = 5, Section = SchoolSection.Primary },
                // Grade 3 - Permanent
                new Grade { Id = 16, Name = "Grade 3", Stream = "Purple", Level = 6, Section = SchoolSection.Primary },
                new Grade { Id = 17, Name = "Grade 3", Stream = "Green", Level = 6, Section = SchoolSection.Primary},
                new Grade { Id = 18, Name = "Grade 3", Stream = "Orange", Level = 6, Section = SchoolSection.Primary },
                // Grade 4 - Permanent
                new Grade { Id = 19, Name = "Grade 4", Stream = "Purple", Level = 7, Section = SchoolSection.Primary },
                new Grade { Id = 20, Name = "Grade 4", Stream = "Green", Level = 7, Section = SchoolSection.Primary},
                new Grade { Id = 21, Name = "Grade 4", Stream = "Orange", Level = 7, Section = SchoolSection.Primary },
                // Grade 5 - Permanent
                new Grade { Id = 22, Name = "Grade 5", Stream = "Purple", Level = 8, Section = SchoolSection.Primary },
                new Grade { Id = 23, Name = "Grade 5", Stream = "Green", Level = 8, Section = SchoolSection.Primary},
                new Grade { Id = 24, Name = "Grade 5", Stream = "Orange", Level = 8, Section = SchoolSection.Primary },
                // Grade 6 - Permanent
                new Grade { Id = 25, Name = "Grade 6", Stream = "Purple", Level = 9, Section = SchoolSection.Primary },
                new Grade { Id = 26, Name = "Grade 6", Stream = "Green", Level = 9, Section = SchoolSection.Primary},
                new Grade { Id = 27, Name = "Grade 6", Stream = "Orange", Level = 9, Section = SchoolSection.Primary },
                
                // Grade 7
                new Grade { Id = 28, Name = "Grade 7", Stream = "Purple", Level = 10, Section = SchoolSection.Primary },
                new Grade { Id = 29, Name = "Grade 7", Stream = "Green", Level = 10, Section = SchoolSection.Primary},
                new Grade { Id = 30, Name = "Grade 7", Stream = "Orange", Level = 10, Section = SchoolSection.Primary},

                // SECONDARY SECTION 
                new Grade { Id = 31, Name = "Grade 9", Stream = "Grey", Level = 11, Section = SchoolSection.Secondary},
                new Grade { Id = 32, Name = "Grade 9", Stream = "Blue", Level = 11, Section = SchoolSection.Secondary},
                new Grade { Id = 33, Name = "Grade 10", Stream = "Grey", Level = 12, Section = SchoolSection.Secondary},
                new Grade { Id = 34, Name = "Grade 10", Stream = "Blue", Level = 12, Section = SchoolSection.Secondary},
                new Grade { Id = 35, Name = "Grade 11", Stream = "Grey", Level = 13, Section = SchoolSection.Secondary},
                new Grade { Id = 36, Name = "Grade 11", Stream = "Blue", Level = 13, Section = SchoolSection.Secondary},
                new Grade { Id = 37, Name = "Grade 12", Stream = "Grey", Level = 14, Section = SchoolSection.Secondary},
                new Grade { Id = 38, Name = "Grade 12", Stream = "Blue", Level = 14, Section = SchoolSection.Secondary}

                
                
               
                
          
            );
        }
    }
}