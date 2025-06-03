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
                _logger.LogInformation("🌱 Starting database seeding...");

                // Check if database is accessible
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    _logger.LogError("❌ Cannot connect to database");
                    return;
                }

                _logger.LogInformation("✅ Database connection successful");

                // Count existing users
                var userCount = await _context.Users.CountAsync();
                _logger.LogInformation($"📊 Current user count: {userCount}");

                // List existing users
                var existingUsers = await _context.Users.Select(u => new { u.Username, u.Role, u.IsActive }).ToListAsync();
                foreach (var user in existingUsers)
                {
                    _logger.LogInformation($"👤 Existing user: {user.Username} ({user.Role}) - Active: {user.IsActive}");
                }

                // Seed sample teacher if not exists
                var teacherExists = await _context.Users.AnyAsync(u => u.Username == "mwiindec");
                _logger.LogInformation($"🔍 Teacher 'mwiindec' exists: {teacherExists}");

                if (!teacherExists)
                {
                    _logger.LogInformation("➕ Creating teacher user...");
                    
                    var passwordHash = BCrypt.Net.BCrypt.HashPassword("mwiindec123");
                    _logger.LogInformation($"🔐 Generated password hash: {passwordHash.Substring(0, 20)}...");

                    var teacherUser = new User
                    {
                        Username = "mwiindec",
                        PasswordHash = passwordHash,
                        FullName = "Clement Mwiinde",
                        Email = "clement.mwiinde@chs.edu",
                        Role = UserRole.Teacher,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(teacherUser);
                    
                    try
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("✅ Teacher created successfully");
                        
                        // Verify the user was created
                        var createdUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "mwiindec");
                        if (createdUser != null)
                        {
                            _logger.LogInformation($"✅ Verification: Teacher found with ID {createdUser.Id}");
                            
                            // Test password verification
                            var passwordTest = BCrypt.Net.BCrypt.Verify("mwiindec123", createdUser.PasswordHash);
                            _logger.LogInformation($"🔐 Password verification test: {passwordTest}");
                        }
                        else
                        {
                            _logger.LogError("❌ Teacher was not found after creation");
                        }
                    }
                    catch (Exception saveEx)
                    {
                        _logger.LogError(saveEx, "❌ Failed to save teacher user");
                        throw;
                    }
                }
                else
                {
                    _logger.LogInformation("ℹ️ Teacher already exists, skipping creation");
                    
                    // Verify existing teacher
                    var existingTeacher = await _context.Users.FirstOrDefaultAsync(u => u.Username == "mwiindec");
                    if (existingTeacher != null)
                    {
                        _logger.LogInformation($"📋 Existing teacher details:");
                        _logger.LogInformation($"   - ID: {existingTeacher.Id}");
                        _logger.LogInformation($"   - Username: {existingTeacher.Username}");
                        _logger.LogInformation($"   - FullName: {existingTeacher.FullName}");
                        _logger.LogInformation($"   - Role: {existingTeacher.Role}");
                        _logger.LogInformation($"   - IsActive: {existingTeacher.IsActive}");
                        _logger.LogInformation($"   - CreatedAt: {existingTeacher.CreatedAt}");
                        
                        // Test password verification on existing user
                        var passwordTest = BCrypt.Net.BCrypt.Verify("mwiindec123", existingTeacher.PasswordHash);
                        _logger.LogInformation($"🔐 Existing user password verification: {passwordTest}");
                        
                        if (!passwordTest)
                        {
                            _logger.LogWarning("⚠️ Password verification failed for existing teacher - updating password");
                            existingTeacher.PasswordHash = BCrypt.Net.BCrypt.HashPassword("mwiindec123");
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("✅ Teacher password updated");
                        }
                    }
                }

                // Add sample student if not exists
                var studentCount = await _context.Students.CountAsync();
                _logger.LogInformation($"📊 Current student count: {studentCount}");

                if (studentCount == 0)
                {
                    _logger.LogInformation("➕ Creating sample student...");
                    
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
                    
                    try
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("✅ Sample student created successfully");
                    }
                    catch (Exception studentEx)
                    {
                        _logger.LogError(studentEx, "❌ Failed to create sample student");
                        // Don't throw - student creation is not critical
                    }
                }
                else
                {
                    _logger.LogInformation("ℹ️ Students already exist, skipping student creation");
                }

                // Final verification
                var finalUserCount = await _context.Users.CountAsync();
                var finalStudentCount = await _context.Students.CountAsync();
                
                _logger.LogInformation($"🏁 Database seeding completed successfully");
                _logger.LogInformation($"📊 Final counts - Users: {finalUserCount}, Students: {finalStudentCount}");

                // List all users for verification
                var allUsers = await _context.Users.Select(u => new { u.Username, u.Role, u.IsActive }).ToListAsync();
                _logger.LogInformation("👥 All users in system:");
                foreach (var user in allUsers)
                {
                    _logger.LogInformation($"   - {user.Username} ({user.Role}) - Active: {user.IsActive}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error occurred during database seeding");
                throw;
            }
        }
    }
}