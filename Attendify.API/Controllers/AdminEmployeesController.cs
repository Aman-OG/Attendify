// Attendify.API/Controllers/AdminEmployeesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Attendify.DATA;
using Attendify.DATA.Models;
using System.Threading.Tasks;
using System.Linq;

namespace Attendify.API.Controllers
{
    [Route("api/admin/employees")]
    [ApiController]
    public class AdminEmployeesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminEmployeesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/employees
        // Supports filtering, searching, sorting & pagination
        [HttpGet]
        public async Task<ActionResult<object>> GetEmployees(
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "EmpCode",
            [FromQuery] string? sortOrder = "asc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = _context.Employees.AsQueryable();

            // Search only
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(e =>
                    e.EmpCode.ToLower().Contains(term) ||
                    e.FirstName.ToLower().Contains(term) ||
                    e.LastName.ToLower().Contains(term) ||
                    (e.Email != null && e.Email.ToLower().Contains(term)));
            }

            // Sorting
            query = sortBy?.ToLower() switch
            {
                "firstname" => sortOrder == "desc" ? query.OrderByDescending(e => e.FirstName) : query.OrderBy(e => e.FirstName),
                "lastname" => sortOrder == "desc" ? query.OrderByDescending(e => e.LastName) : query.OrderBy(e => e.LastName),
                "department" => sortOrder == "desc" ? query.OrderByDescending(e => e.Department) : query.OrderBy(e => e.Department),
                "position" => sortOrder == "desc" ? query.OrderByDescending(e => e.Position) : query.OrderBy(e => e.Position),
                _ => sortOrder == "desc" ? query.OrderByDescending(e => e.EmpCode) : query.OrderBy(e => e.EmpCode),
            };

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new
                {
                    e.EmployeeID,
                    e.EmpCode,
                    e.FirstName,
                    e.MiddleName,
                    e.LastName,
                    e.Department,
                    e.Position,
                    e.Email,
                    e.Phone,
                    e.Role,
                    e.IsActive
                })
                .ToListAsync();

            return Ok(new
            {
                data = items,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }

        [HttpGet("{empCode}")]
        public async Task<ActionResult<Employee>> GetEmployee(string empCode)
        {
            var emp = await _context.Employees.FirstOrDefaultAsync(e => e.EmpCode == empCode);
            return emp == null ? NotFound() : Ok(emp);
        }

        [HttpPost]
        public async Task<ActionResult<Employee>> CreateEmployee([FromBody] Employee employee)
        {
            Console.WriteLine("=== START CreateEmployee ===");
            Console.WriteLine($"Received EmpCode: {employee.EmpCode}");
            Console.WriteLine($"Received Email: {employee.Email}");

            try
            {
                // 1. VALIDATION
                if (string.IsNullOrWhiteSpace(employee.EmpCode))
                {
                    Console.WriteLine("Validation failed: EmpCode is empty");
                    return BadRequest(new { message = "Employee ID (EmpCode) is required" });
                }

                Console.WriteLine($"Checking if EmpCode '{employee.EmpCode}' already exists...");
                bool empCodeExists = await _context.Employees.AnyAsync(e => e.EmpCode == employee.EmpCode);
                Console.WriteLine($"EmpCode exists check: {empCodeExists}");

                if (empCodeExists)
                {
                    Console.WriteLine($"Conflict: EmpCode '{employee.EmpCode}' already exists");
                    return Conflict(new { message = "Employee ID already exists" });
                }

                if (!string.IsNullOrWhiteSpace(employee.Email))
                {
                    Console.WriteLine($"Checking if Email '{employee.Email}' already exists...");
                    bool emailExists = await _context.Employees.AnyAsync(e => e.Email == employee.Email);
                    Console.WriteLine($"Email exists check: {emailExists}");

                    if (emailExists)
                    {
                        Console.WriteLine($"Conflict: Email '{employee.Email}' already in use");
                        return Conflict(new { message = "Email already in use" });
                    }
                }

                // 2. GENERATE AND HASH PASSWORD
                Console.WriteLine($"Extracting number from EmpCode: {employee.EmpCode}");
                string numberPart = employee.EmpCode.Replace("Emp", "");
                Console.WriteLine($"Number part: {numberPart}");

                string rawPassword = $"Pass{numberPart}";
                Console.WriteLine($"Raw password generated: {rawPassword}");

                Console.WriteLine("Hashing password...");
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(rawPassword);
                Console.WriteLine($"Password hashed successfully. Hash length: {hashedPassword.Length}");
                Console.WriteLine($"PasswordHash before assignment: {employee.PasswordHash ?? "NULL"}");

                employee.PasswordHash = hashedPassword;
                Console.WriteLine($"PasswordHash after assignment: {employee.PasswordHash ?? "NULL"}");

                // 3. SET OTHER PROPERTIES
                employee.CreatedAt = DateTime.UtcNow;
                employee.IsActive = true;
                Console.WriteLine($"IsActive set to: {employee.IsActive}");
                Console.WriteLine($"CreatedAt set to: {employee.CreatedAt}");

                // 4. SAVE TO DATABASE
                Console.WriteLine("Adding employee to context...");
                _context.Employees.Add(employee);

                Console.WriteLine("Saving changes to database...");
                int changes = await _context.SaveChangesAsync();
                Console.WriteLine($"SaveChanges completed. Rows affected: {changes}");

                // 5. RETURN RESPONSE
                Console.WriteLine("Employee created successfully!");
                Console.WriteLine($"=== END CreateEmployee ===");

                return CreatedAtAction(nameof(GetEmployee), new { empCode = employee.EmpCode }, new
                {
                    employee.EmployeeID,
                    employee.EmpCode,
                    employee.FirstName,
                    employee.LastName,
                    employee.Department,
                    employee.Position,
                    employee.Email,
                    employee.Role,
                    employee.Phone,
                    employee.IsActive,
                    employee.CreatedAt
                });
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"=== DATABASE ERROR ===");
                Console.WriteLine($"DbUpdateException: {dbEx.Message}");
                Console.WriteLine($"Inner Exception: {dbEx.InnerException?.Message}");
                Console.WriteLine($"StackTrace: {dbEx.StackTrace}");
                Console.WriteLine($"=== END ERROR ===");

                return StatusCode(500, new
                {
                    message = "Database error while saving employee",
                    error = dbEx.InnerException?.Message ?? dbEx.Message
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== GENERAL ERROR ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                Console.WriteLine($"=== END ERROR ===");

                return StatusCode(500, new
                {
                    message = "An error occurred while creating the employee",
                    error = ex.Message
                });
            }
        }


        [HttpPut("{empCode}")]
        public async Task<IActionResult> UpdateEmployee(string empCode, [FromBody] Employee updated)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmpCode == empCode);
            if (employee == null) return NotFound();

            employee.FirstName = updated.FirstName;
            employee.MiddleName = updated.MiddleName;
            employee.LastName = updated.LastName;
            employee.Department = updated.Department;
            employee.Position = updated.Position;
            employee.Email = updated.Email;
            employee.Phone = updated.Phone;
            employee.Role = updated.Role;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Updated successfully" });
        }

        [HttpDelete("{empCode}")]
        public async Task<IActionResult> DeleteEmployee(string empCode)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmpCode == empCode);
            if (employee == null) return NotFound();

            employee.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Deactivated" });
        }
    }
}