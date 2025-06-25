using BluebirdCore.Data;
using BluebirdCore.DTOs;
using BluebirdCore.Entities;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace BluebirdCore.Services
{
    public interface IStudentService
    {
        Task<IEnumerable<Student>> GetAllStudentsAsync(bool includeArchived = false);
        Task<IEnumerable<Student>> GetStudentsByGradeAsync(int gradeId);
        Task<Student> GetStudentByIdAsync(int id);
        Task<Student> CreateStudentAsync(Student student);
        Task<Student> UpdateStudentAsync(Student student);
        Task<bool> DeleteStudentAsync(int id);
        Task<bool> ArchiveStudentAsync(int id);
        Task<bool> PromoteStudentsAsync(int fromGradeId, int toGradeId);
        //Task<IEnumerable<Student>> ImportStudentsFromCsvAsync(Stream csvStream);
        Task<ImportResult<Student>> ImportStudentsFromCsvAsync(Stream csvStream);
        
        [Obsolete("Use TransitionPromotionService.PromoteStudentAsync or PromoteAllStudentsAsync instead. This method doesn't handle curriculum transitions properly.", false)]
        Task<PromotionResult> PromoteStudentsToNextGradeAsync(int fromGradeId);
    }

    public class StudentService : IStudentService
    {
        private readonly SchoolDbContext _context;
        private readonly ILogger<StudentService>? _logger;

        public StudentService(SchoolDbContext context, ILogger<StudentService>? logger = null)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Student>> GetAllStudentsAsync(bool includeArchived = false)
        {
            var query = _context.Students.Include(s => s.Grade).AsQueryable();

            if (!includeArchived)
                query = query.Where(s => !s.IsArchived);

            return await query.OrderBy(s => s.LastName)
                             .ThenBy(s => s.FirstName)
                             .ToListAsync();
        }

        public async Task<IEnumerable<Student>> GetStudentsByGradeAsync(int gradeId)
        {
            return await _context.Students
                .Include(s => s.Grade)
                .Where(s => s.GradeId == gradeId && !s.IsArchived)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }

        public async Task<Student> GetStudentByIdAsync(int id)
        {
            return await _context.Students
                .Include(s => s.Grade)
                .Include(s => s.OptionalSubjects)
                    .ThenInclude(os => os.Subject)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Student> CreateStudentAsync(Student student)
        {
            // Generate student number if not provided
            if (string.IsNullOrEmpty(student.StudentNumber))
            {
                student.StudentNumber = await GenerateStudentNumberAsync(student.GradeId);
            }

            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            return student;
        }

        public async Task<Student> UpdateStudentAsync(Student student)
        {
            _context.Entry(student).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return student;
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return false;

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ArchiveStudentAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return false;

            student.IsArchived = true;
            student.ArchiveDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PromoteStudentsAsync(int fromGradeId, int toGradeId)
        {
            var students = await _context.Students
                .Where(s => s.GradeId == fromGradeId && !s.IsArchived)
                .ToListAsync();

            foreach (var student in students)
            {
                student.GradeId = toGradeId;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    [Obsolete("Use TransitionPromotionService.PromoteStudentAsync or PromoteAllStudentsAsync instead. This method doesn't handle curriculum transitions properly.", false)] 
    public async Task<PromotionResult> PromoteStudentsToNextGradeAsync(int fromGradeId)
    {
        var result = new PromotionResult();

        try
        {
            // Get the source grade with its details
            var fromGrade = await _context.Grades
                .Include(g => g.Students.Where(s => !s.IsArchived))
                .FirstOrDefaultAsync(g => g.Id == fromGradeId && g.IsActive);

            if (fromGrade == null)
            {
                result.Success = false;
                result.ErrorMessage = "Source grade not found or inactive.";
                return result;
            }

            // Find the target grade (next level, same stream, same section)
            var targetLevel = fromGrade.Level + 1;
            var toGrade = await _context.Grades
                .FirstOrDefaultAsync(g => 
                    g.Level == targetLevel && 
                    g.Stream == fromGrade.Stream && 
                    g.Section == fromGrade.Section && 
                    g.IsActive);

            if (toGrade == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Target grade not found. No active grade found for Level {targetLevel}, Stream '{fromGrade.Stream}', Section '{fromGrade.Section}'.";
                return result;
            }

            // Check if there are students to promote
            var studentsToPromote = fromGrade.Students.Where(s => !s.IsArchived).ToList();
            if (!studentsToPromote.Any())
            {
                result.Success = true;
                result.Message = "No students found to promote.";
                result.StudentsPromoted = 0;
                return result;
            }

            // Promote students
            foreach (var student in studentsToPromote)
            {
                student.GradeId = toGrade.Id;
                result.StudentsPromoted++;
            }

            await _context.SaveChangesAsync();

            result.Success = true;
            result.Message = $"Successfully promoted {result.StudentsPromoted} students from {fromGrade.FullName} to {toGrade.FullName}.";
            result.FromGrade = fromGrade.FullName;
            result.ToGrade = toGrade.FullName;

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"An error occurred during promotion: {ex.Message}";
            return result;
        }
    }

        public async Task<ImportResult<Student>> ImportStudentsFromCsvAsync(Stream csvStream)
        {
            if (csvStream == null)
                throw new ArgumentNullException(nameof(csvStream));

            var students = new List<Student>();
            var errors = new List<string>();
            var studentNumbersInImport = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // For batch StudentNumber generation
            var dtosWithMissingNumber = new List<(StudentDto dto, int rowNumber)>();
            var allStudentDtos = new List<(StudentDto dto, int rowNumber)>();

            int totalRows = 0;

            try
            {
                using var reader = new StreamReader(csvStream);
                using var csv = new CsvHelper.CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null,
                    TrimOptions = CsvHelper.Configuration.TrimOptions.Trim,
                    BadDataFound = context =>
                    {
                        errors.Add($"Row {context.RawRecord}: Invalid data in field '{context.Field}'");
                    }
                });

                // Read and validate headers
                await csv.ReadAsync();
                csv.ReadHeader();
                var headerRecord = csv.HeaderRecord;

                ValidateRequiredHeaders(headerRecord, errors);

                if (errors.Any())
                {
                    return new ImportResult<Student>
                    {
                        Successful = 0,
                        Failed = 0,
                        Total = 0,
                        Errors = errors,
                        Imported = new List<Student>()
                    };
                }

                var rowNumber = 1;
                while (await csv.ReadAsync())
                {
                    rowNumber++;
                    totalRows++;
                    try
                    {
                        var studentDto = await ParseStudentFromCsvRow(csv, rowNumber, errors, generateStudentNumber: false);
                        if (studentDto != null)
                        {
                            if (string.IsNullOrWhiteSpace(studentDto.StudentNumber))
                            {
                                dtosWithMissingNumber.Add((studentDto, rowNumber));
                            }
                            else
                            {
                                // Check for duplicate StudentNumber in the import batch
                                if (!studentNumbersInImport.Add(studentDto.StudentNumber))
                                {
                                    errors.Add($"Row {rowNumber}: Duplicate StudentNumber '{studentDto.StudentNumber}' in import file.");
                                    continue;
                                }
                            }
                            allStudentDtos.Add((studentDto, rowNumber));
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {rowNumber}: Error processing row - {ex.Message}");
                    }
                }

                // Batch-generate StudentNumbers for missing ones, ensuring uniqueness in batch and DB
                if (dtosWithMissingNumber.Any())
                {
                    var gradeIds = dtosWithMissingNumber.Select(x => x.dto.GradeId).Distinct().ToList();
                    var currentYear = DateTime.Now.Year;
                    var prefixes = gradeIds.Select(gid => $"{currentYear}{gid:D2}").ToList();

                    var existingNumbers = await _context.Students
                        .Where(s => prefixes.Any(p => s.StudentNumber.StartsWith(p)))
                        .Select(s => s.StudentNumber)
                        .ToListAsync();

                    var allUsedNumbers = new HashSet<string>(existingNumbers, StringComparer.OrdinalIgnoreCase);
                    allUsedNumbers.UnionWith(studentNumbersInImport);

                    // Find the max sequence for each prefix
                    var prefixCounters = prefixes.ToDictionary(
                        p => p,
                        p =>
                        {
                            var maxSeq = existingNumbers
                                .Where(sn => sn.StartsWith(p) && sn.Length >= 10)
                                .Select(sn =>
                                {
                                    if (int.TryParse(sn.Substring(6), out var seq))
                                        return seq;
                                    return 0;
                                })
                                .DefaultIfEmpty(0)
                                .Max();
                            return maxSeq + 1;
                        });

                    foreach (var (dto, dtoRowNumber) in dtosWithMissingNumber)
                    {
                        var prefix = $"{currentYear}{dto.GradeId:D2}";
                        int seq = prefixCounters[prefix];
                        string newNumber;
                        do
                        {
                            newNumber = $"{prefix}{seq:D4}";
                            seq++;
                        } while (allUsedNumbers.Contains(newNumber));
                        prefixCounters[prefix] = seq;
                        dto.StudentNumber = newNumber;
                        allUsedNumbers.Add(newNumber);
                        studentNumbersInImport.Add(newNumber);
                    }
                }

                // Now, map DTOs to entities
                foreach (var (studentDto, dtoRowNumber) in allStudentDtos)
                {
                    var student = await MapToStudentEntity(studentDto, dtoRowNumber, errors, skipStudentNumberCheck: true);
                    if (student != null)
                    {
                        students.Add(student);
                    }
                }

                // Validate for duplicates within the import batch and other business rules
                var validationErrors = await ValidateImportDataAsync(students);
                errors.AddRange(validationErrors);

                // Check for StudentNumbers that already exist in the database (bulk check)
                var importStudentNumbers = students.Select(s => s.StudentNumber).ToList();
                var existingStudentNumbers = await _context.Students
                    .Where(s => importStudentNumbers.Contains(s.StudentNumber))
                    .Select(s => s.StudentNumber)
                    .ToListAsync();

                foreach (var dup in existingStudentNumbers)
                {
                    errors.Add($"StudentNumber '{dup}' already exists in the database.");
                }

                // Remove students with duplicate StudentNumbers (either in import or DB)
                students = students.Where(s =>
                    !existingStudentNumbers.Contains(s.StudentNumber) &&
                    studentNumbersInImport.Contains(s.StudentNumber)
                ).ToList();

                if (errors.Any())
                {
                    _logger?.LogWarning("CSV import completed with errors: {Errors}", string.Join("; ", errors));
                }

                // Save valid students to database
                if (students.Any())
                {
                    _context.Students.AddRange(students);
                    try
                    {
                        await _context.SaveChangesAsync();
                        _logger?.LogInformation("Successfully saved {Count} students to database", students.Count);
                    }
                    catch (DbUpdateException dbEx)
                    {
                        _logger?.LogError(dbEx, "DbUpdateException during student import. This may be due to duplicate StudentNumbers or other DB constraints.");
                        errors.Add("A database error occurred during import. This may be due to duplicate StudentNumbers or other DB constraints.");
                    }
                }

                return new ImportResult<Student>
                {
                    Successful = students.Count,
                    Failed = totalRows - students.Count,
                    Total = totalRows,
                    Errors = errors,
                    Imported = students
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to import students from CSV");
                return new ImportResult<Student>
                {
                    Successful = 0,
                    Failed = totalRows,
                    Total = totalRows,
                    Errors = new List<string> { $"CSV import failed: {ex.Message}" },
                    Imported = new List<Student>()
                };
            }
        }

        private void ValidateRequiredHeaders(string[] headers, List<string> errors)
        {
            var requiredHeaders = new[]
            {
                "FirstName", "LastName", "GradeId"
            };

            var missingHeaders = requiredHeaders.Where(h =>
                !headers.Any(header => string.Equals(header, h, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (missingHeaders.Any())
            {
                errors.Add($"Missing required headers: {string.Join(", ", missingHeaders)}");
            }
        }

        // Overload for batch import: optionally skip StudentNumber generation
        private async Task<StudentDto> ParseStudentFromCsvRow(CsvReader csv, int rowNumber, List<string> errors, bool generateStudentNumber = true)
        {
            try
            {
                var dto = new StudentDto
                {
                    FirstName = GetCsvValue(csv, "FirstName")?.Trim(),
                    LastName = GetCsvValue(csv, "LastName")?.Trim(),
                    MiddleName = GetCsvValue(csv, "MiddleName")?.Trim(),
                    StudentNumber = GetCsvValue(csv, "StudentNumber")?.Trim(),
                    Gender = GetCsvValue(csv, "Gender")?.Trim(),
                    Address = GetCsvValue(csv, "Address")?.Trim(),
                    PhoneNumber = GetCsvValue(csv, "PhoneNumber")?.Trim(),
                    GuardianName = GetCsvValue(csv, "GuardianName")?.Trim(),
                    GuardianPhone = GetCsvValue(csv, "GuardianPhone")?.Trim(),
                    GradeName = GetCsvValue(csv, "GradeName")?.Trim(),
                    IsActive = ParseBoolean(GetCsvValue(csv, "IsActive"), true),
                    IsArchived = ParseBoolean(GetCsvValue(csv, "IsArchived"), false)
                };

                if (TryParseDate(GetCsvValue(csv, "DateOfBirth"), out var dateOfBirth))
                {
                    dto.DateOfBirth = dateOfBirth;
                }
                else
                {
                    dto.DateOfBirth = DateTime.UtcNow.AddYears(-17);
                }

                if (TryParseDate(GetCsvValue(csv, "EnrollmentDate"), out var enrollmentDate))
                {
                    dto.EnrollmentDate = enrollmentDate;
                }
                else
                {
                    dto.EnrollmentDate = DateTime.UtcNow;
                }

                if (!int.TryParse(GetCsvValue(csv, "GradeId"), out var gradeId))
                {
                    errors.Add($"Row {rowNumber}: Invalid GradeId format");
                    return null;
                }
                dto.GradeId = gradeId;

                if (string.IsNullOrWhiteSpace(dto.FirstName))
                {
                    errors.Add($"Row {rowNumber}: FirstName is required");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(dto.LastName))
                {
                    errors.Add($"Row {rowNumber}: LastName is required");
                    return null;
                }

                if (generateStudentNumber && string.IsNullOrWhiteSpace(dto.StudentNumber))
                {
                    dto.StudentNumber = await GenerateStudentNumberAsync(dto.GradeId);
                }

                dto.FullName = $"{dto.FirstName} {dto.MiddleName} {dto.LastName}".Replace("  ", " ").Trim();
                dto.OptionalSubjects = new List<SubjectDto>();

                return dto;
            }
            catch (Exception ex)
            {
                errors.Add($"Row {rowNumber}: Error parsing data - {ex.Message}");
                return null;
            }
        }

        private string GetCsvValue(CsvReader csv, string fieldName)
        {
            try
            {
                return csv.GetField(fieldName);
            }
            catch
            {
                return null;
            }
        }

        private bool TryParseDate(string dateString, out DateTime date)
        {
            date = default;

            if (string.IsNullOrWhiteSpace(dateString))
                return false;

            var formats = new[]
            {
                "yyyy-MM-dd",
                "MM/dd/yyyy",
                "dd/MM/yyyy",
                "yyyy/MM/dd",
                "MM-dd-yyyy",
                "dd-MM-yyyy"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return true;
                }
            }

            return DateTime.TryParse(dateString, out date);
        }

        private bool ParseBoolean(string value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            var normalizedValue = value.Trim().ToLowerInvariant();
            return normalizedValue switch
            {
                "true" or "1" or "yes" or "y" or "active" => true,
                "false" or "0" or "no" or "n" or "inactive" => false,
                _ => defaultValue
            };
        }

        // Overload for batch import: optionally skip StudentNumber existence check
        private async Task<Student> MapToStudentEntity(StudentDto dto, int rowNumber, List<string> errors, bool skipStudentNumberCheck = false)
        {
            try
            {
                if (!skipStudentNumberCheck && !string.IsNullOrWhiteSpace(dto.StudentNumber))
                {
                    var existingStudent = await _context.Students
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.StudentNumber == dto.StudentNumber);

                    if (existingStudent != null)
                    {
                        errors.Add($"Row {rowNumber}: Student with number {dto.StudentNumber} already exists");
                        return null;
                    }
                }

                var grade = await _context.Grades.FindAsync(dto.GradeId);
                if (grade == null)
                {
                    errors.Add($"Row {rowNumber}: Grade with ID {dto.GradeId} not found");
                    return null;
                }

                var student = new Student
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    MiddleName = dto.MiddleName,
                    StudentNumber = dto.StudentNumber,
                    DateOfBirth = dto.DateOfBirth,
                    Gender = dto.Gender,
                    Address = dto.Address,
                    PhoneNumber = dto.PhoneNumber,
                    GuardianName = dto.GuardianName,
                    GuardianPhone = dto.GuardianPhone,
                    GradeId = dto.GradeId,
                    IsActive = dto.IsActive,
                    IsArchived = dto.IsArchived,
                    EnrollmentDate = dto.EnrollmentDate
                };

                return student;
            }
            catch (Exception ex)
            {
                errors.Add($"Row {rowNumber}: Error mapping to entity - {ex.Message}");
                return null;
            }
        }

        private async Task<string> GenerateStudentNumberAsync(int gradeId)
        {
            var currentYear = DateTime.Now.Year;
            var gradePrefix = $"{currentYear}{gradeId:D2}";

            var lastStudentNumber = await _context.Students
                .Where(s => s.StudentNumber.StartsWith(gradePrefix))
                .OrderByDescending(s => s.StudentNumber)
                .Select(s => s.StudentNumber)
                .FirstOrDefaultAsync();

            int nextSequence = 1;
            if (!string.IsNullOrEmpty(lastStudentNumber) && lastStudentNumber.Length >= 8)
            {
                var sequencePart = lastStudentNumber.Substring(6);
                if (int.TryParse(sequencePart, out var currentSequence))
                {
                    nextSequence = currentSequence + 1;
                }
            }

            return $"{gradePrefix}{nextSequence:D4}";
        }

        private async Task<List<string>> ValidateImportDataAsync(IEnumerable<Student> students)
        {
            var validationErrors = new List<string>();
            var studentNumbers = new HashSet<string>();

            foreach (var student in students)
            {
                if (!studentNumbers.Add(student.StudentNumber))
                {
                    validationErrors.Add($"Duplicate StudentNumber in import: {student.StudentNumber}");
                }

                if (student.EnrollmentDate > DateTime.Now)
                {
                    validationErrors.Add($"Student {student.StudentNumber}: Enrollment date cannot be in the future");
                }

                if (student.DateOfBirth != null && student.DateOfBirth > DateTime.Now.AddYears(-3))
                {
                    validationErrors.Add($"Student {student.StudentNumber}: Date of birth seems too recent");
                }
            }

            return validationErrors;
        }
    }
}

public class PromotionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int StudentsPromoted { get; set; }
    public string? FromGrade { get; set; }
    public string? ToGrade { get; set; }
}