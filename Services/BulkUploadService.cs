using System.Text;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using BluebirdCore.Entities;
using BluebirdCore.Data;
using Microsoft.EntityFrameworkCore;

namespace SchoolManagementSystem.Services
{

    public class BulkUploadResult<T>
    {
        public int TotalRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int FailedRecords { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<T> ImportedRecords { get; set; } = new List<T>();
    }

    public interface IBulkUploadService
    {
        Task<BulkUploadResult<Student>> ImportStudentsFromCsvAsync(Stream csvStream);
        Task<BulkUploadResult<Student>> ImportStudentsFromExcelAsync(Stream excelStream);
        Task<BulkUploadResult<Subject>> ImportSubjectsFromCsvAsync(Stream csvStream);
        Task<BulkUploadResult<Subject>> ImportSubjectsFromExcelAsync(Stream excelStream);
    }

    public class BulkUploadService : IBulkUploadService
    {
        private readonly SchoolDbContext _context;
        private readonly ILogger<BulkUploadService> _logger;

        public BulkUploadService(SchoolDbContext context, ILogger<BulkUploadService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BulkUploadResult<Student>> ImportStudentsFromCsvAsync(Stream csvStream)
        {
            var result = new BulkUploadResult<Student>();
            
            try
            {
                using var reader = new StreamReader(csvStream);
                var csvContent = await reader.ReadToEndAsync();
                var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                if (lines.Length == 0)
                {
                    result.Errors.Add("CSV file is empty");
                    return result;
                }

                // Parse header
                var header = lines[0].Split(',').Select(h => h.Trim().Trim('"')).ToArray();
                result.TotalRecords = lines.Length - 1;

                // Validate required columns
                var requiredColumns = new[] { "FirstName", "LastName", "DateOfBirth", "GradeId" };
                var missingColumns = requiredColumns.Where(col => !header.Contains(col, StringComparer.OrdinalIgnoreCase)).ToList();
                
                if (missingColumns.Any())
                {
                    result.Errors.Add($"Missing required columns: {string.Join(", ", missingColumns)}");
                    return result;
                }

                // Process data rows
                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        var values = ParseCsvLine(lines[i]);
                        if (values.Length != header.Length)
                        {
                            result.Errors.Add($"Row {i}: Column count mismatch");
                            result.FailedRecords++;
                            continue;
                        }

                        var student = new Student
                        {
                            FirstName = string.Empty, // Placeholder value
                            LastName = string.Empty   // Placeholder value
                        };
                        
                        // Map CSV columns to student properties
                        for (int j = 0; j < header.Length; j++)
                        {
                            var columnName = header[j];
                            var value = values[j]?.Trim().Trim('"');

                            switch (columnName.ToLower())
                            {
                                case "firstname":
                                    student.FirstName = value;
                                    break;
                                case "lastname":
                                    student.LastName = value;
                                    break;
                                case "middlename":
                                    student.MiddleName = value;
                                    break;
                                case "studentnumber":
                                    student.StudentNumber = value;
                                    break;
                                case "dateofbirth":
                                    if (DateTime.TryParse(value, out var dob))
                                        student.DateOfBirth = dob;
                                    else
                                        throw new ArgumentException($"Invalid date format: {value}");
                                    break;
                                case "gender":
                                    student.Gender = value;
                                    break;
                                case "address":
                                    student.Address = value;
                                    break;
                                case "phonenumber":
                                    student.PhoneNumber = value;
                                    break;
                                case "guardianname":
                                    student.GuardianName = value;
                                    break;
                                case "guardianphone":
                                    student.GuardianPhone = value;
                                    break;
                                case "gradeid":
                                    if (int.TryParse(value, out var gradeId))
                                        student.GradeId = gradeId;
                                    else
                                        throw new ArgumentException($"Invalid Grade ID: {value}");
                                    break;
                            }
                        }

                        // Validate required fields
                        if (string.IsNullOrEmpty(student.FirstName) || string.IsNullOrEmpty(student.LastName))
                        {
                            result.Errors.Add($"Row {i}: FirstName and LastName are required");
                            result.FailedRecords++;
                            continue;
                        }

                        // Generate student number if not provided
                        if (string.IsNullOrEmpty(student.StudentNumber))
                        {
                            student.StudentNumber = await GenerateStudentNumberAsync();
                        }

                        // Check if student number already exists
                        if (await _context.Students.AnyAsync(s => s.StudentNumber == student.StudentNumber))
                        {
                            result.Errors.Add($"Row {i}: Student number {student.StudentNumber} already exists");
                            result.FailedRecords++;
                            continue;
                        }

                        // Verify grade exists
                        if (!await _context.Grades.AnyAsync(g => g.Id == student.GradeId))
                        {
                            result.Errors.Add($"Row {i}: Grade ID {student.GradeId} does not exist");
                            result.FailedRecords++;
                            continue;
                        }

                        student.EnrollmentDate = DateTime.UtcNow;
                        student.IsActive = true;

                        _context.Students.Add(student);
                        result.ImportedRecords.Add(student);
                        result.SuccessfulRecords++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Row {i}: {ex.Message}");
                        result.FailedRecords++;
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Bulk import completed: {result.SuccessfulRecords} successful, {result.FailedRecords} failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CSV import");
                result.Errors.Add($"Import failed: {ex.Message}");
            }

            return result;
        }

        public async Task<BulkUploadResult<Student>> ImportStudentsFromExcelAsync(Stream excelStream)
        {
            var result = new BulkUploadResult<Student>();

            try
            {
                // For Excel import, you would use EPPlus or similar library
                // This is a placeholder implementation
                result.Errors.Add("Excel import requires EPPlus package. Please use CSV format or install EPPlus.");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Excel import");
                result.Errors.Add($"Import failed: {ex.Message}");
                return result;
            }
        }

        public async Task<BulkUploadResult<Subject>> ImportSubjectsFromCsvAsync(Stream csvStream)
        {
            var result = new BulkUploadResult<Subject>();

            try
            {
                using var reader = new StreamReader(csvStream);
                var csvContent = await reader.ReadToEndAsync();
                var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length == 0)
                {
                    result.Errors.Add("CSV file is empty");
                    return result;
                }

                // Parse header
                var header = lines[0].Split(',').Select(h => h.Trim().Trim('"')).ToArray();
                result.TotalRecords = lines.Length - 1;

                // Validate required columns
                var requiredColumns = new[] { "Name" };
                var missingColumns = requiredColumns.Where(col => !header.Contains(col, StringComparer.OrdinalIgnoreCase)).ToList();

                if (missingColumns.Any())
                {
                    result.Errors.Add($"Missing required columns: {string.Join(", ", missingColumns)}");
                    return result;
                }

                // Process data rows
                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        var values = ParseCsvLine(lines[i]);
                        if (values.Length != header.Length)
                        {
                            result.Errors.Add($"Row {i}: Column count mismatch");
                            result.FailedRecords++;
                            continue;
                        }

                        var subject = new Subject
                        {
                            Name = string.Empty // Placeholder value to satisfy the required property
                        };

                        // Map CSV columns to subject properties
                        for (int j = 0; j < header.Length; j++)
                        {
                            var columnName = header[j];
                            var value = values[j]?.Trim().Trim('"');

                            switch (columnName.ToLower())
                            {
                                case "name":
                                    subject.Name = value;
                                    break;
                                case "code":
                                    subject.Code = value;
                                    break;
                                case "description":
                                    subject.Description = value;
                                    break;
                            }
                        }

                        // Validate required fields
                        if (string.IsNullOrEmpty(subject.Name))
                        {
                            result.Errors.Add($"Row {i}: Subject name is required");
                            result.FailedRecords++;
                            continue;
                        }

                        // Check if subject already exists
                        if (await _context.Subjects.AnyAsync(s => s.Name.ToLower() == subject.Name.ToLower()))
                        {
                            result.Errors.Add($"Row {i}: Subject {subject.Name} already exists");
                            result.FailedRecords++;
                            continue;
                        }

                        // Generate code if not provided
                        if (string.IsNullOrEmpty(subject.Code))
                        {
                            subject.Code = GenerateSubjectCode(subject.Name);
                        }

                        subject.IsActive = true;

                        _context.Subjects.Add(subject);
                        result.ImportedRecords.Add(subject);
                        result.SuccessfulRecords++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Row {i}: {ex.Message}");
                        result.FailedRecords++;
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Subject bulk import completed: {result.SuccessfulRecords} successful, {result.FailedRecords} failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during subject CSV import");
                result.Errors.Add($"Import failed: {ex.Message}");
            }

            return result;
        }

        public async Task<BulkUploadResult<Subject>> ImportSubjectsFromExcelAsync(Stream excelStream)
        {
            var result = new BulkUploadResult<Subject>();

            try
            {
                // Placeholder for Excel implementation
                result.Errors.Add("Excel import requires EPPlus package. Please use CSV format or install EPPlus.");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Excel import");
                result.Errors.Add($"Import failed: {ex.Message}");
                return result;
            }
        }

        // Helper methods
        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }

        private async Task<string> GenerateStudentNumberAsync()
        {
            var year = DateTime.Now.Year.ToString().Substring(2);
            var lastStudent = await _context.Students
                .Where(s => s.StudentNumber.StartsWith(year))
                .OrderByDescending(s => s.StudentNumber)
                .FirstOrDefaultAsync();

            var nextNumber = 1;
            if (lastStudent != null && int.TryParse(lastStudent.StudentNumber.Substring(2), out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }

            return $"{year}{nextNumber:D4}";
        }

        private string GenerateSubjectCode(string subjectName)
        {
            // Generate a code from the subject name
            var words = subjectName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1)
            {
                return words[0].Length >= 3 ? words[0].Substring(0, 3).ToUpper() : words[0].ToUpper();
            }
            else
            {
                return string.Join("", words.Take(2).Select(w => w.Substring(0, Math.Min(2, w.Length)))).ToUpper();
            }
        }
    }
}