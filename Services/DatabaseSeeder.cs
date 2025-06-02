using BluebirdCore.Data;
using BluebirdCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace BluebirdCore.Services
{
    public interface IDatabaseSeeder
    {
        Task SeedAsync();
    }

    public class DatabaseSeeder : IDatabaseSeeder
    {
        private readonly SchoolDbContext _context;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(SchoolDbContext context, ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                // Check if we need to add any additional data beyond what's in HasData
                // The admin user is now seeded via HasData with a static hash
                
                // Seed sample teacher if not exists
                if (!await _context.Users.AnyAsync(u => u.Username == "mwiindec"))
                {
                    var teacherUser = new User
                    {
                        Username = "mwiindec",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("mwiindec123"),
                        FullName = "Clement Mwiinde",
                        Email = "clement.mwiinde@chs.edu",
                        Role = UserRole.Teacher,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(teacherUser);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Sample teacher created successfully");
                }

                // Add sample student if not exists
                if (!await _context.Students.AnyAsync())
                {
                    var sampleStudent = new Student
                    {
                        FirstName = "John",
                        LastName = "Mbuki", 
                        StudentNumber = "24001",
                        DateOfBirth = new DateTime(2010, 5, 15),
                        Gender = "Male",
                        GradeId = 2, // Grade 1
                        GuardianName = "John Doe",
                        GuardianPhone = "+260123456789",
                        Address = "123 Sample Street, Lusaka",
                        EnrollmentDate = DateTime.UtcNow
                    };

                    _context.Students.Add(sampleStudent);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Sample student created successfully");
                }

                _logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during database seeding");
                throw;
            }
        }
    }
}
