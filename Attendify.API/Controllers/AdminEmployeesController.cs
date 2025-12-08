// Attendify.API/Controllers/AdminEmployeesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Attendify.DATA;
using Attendify.DATA.Models;
using Attendify.API.Services;  // Add this
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;

namespace Attendify.API.Controllers
{
    [Route("api/admin/employees")]
    [ApiController]
    public class AdminEmployeesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;  // Add this

        public AdminEmployeesController(AppDbContext context, IPasswordHasher passwordHasher)  // Update constructor
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // Helper method to generate password from EmpCode
        private string GeneratePasswordFromEmpCode(string empCode)
        {
            // Extract numeric part from EmpCode (e.g., "Emp00012" -> "00012")
            var numericPart = Regex.Replace(empCode, @"[^\d]", "");
            return $"Pass{numericPart}";
        }

        // GET: api/admin/employees
        [HttpGet]
        public async Task<ActionResult<object>> GetEmployees(
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "EmpCode",
            [FromQuery] string? sortOrder = "asc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = _context.Employees.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(e =>
                    e.EmpCode.ToLower().Contains(term) ||
                    e.FirstName.ToLower().Contains(term) ||
                    e.LastName.ToLower().Contains(term) ||
                    (e.Email != null && e.Email.ToLower().Contains(term)));
            }

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
        public async Task<ActionResult<object>> GetEmployee(string empCode)
        {
            var emp = await _context.Employees
                .Where(e => e.EmpCode == empCode)
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
                    e.IsActive,
                    e.CreatedAt
                })
                .FirstOrDefaultAsync();

            return emp == null ? NotFound() : Ok(emp);
        }

        // Request DTO for Create
        public class CreateEmployeeRequest
        {
            public string EmpCode { get; set; }
            public string FirstName { get; set; }
            public string? MiddleName { get; set; }
            public string LastName { get; set; }
            public string? Department { get; set; }
            public string? Position { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? Role { get; set; }
        }

        [HttpPost]
        public async Task<ActionResult<object>> CreateEmployee([FromBody] CreateEmployeeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.EmpCode))
                return BadRequest(new { message = "Employee ID (EmpCode) is required" });

            if (await _context.Employees.AnyAsync(e => e.EmpCode == request.EmpCode))
                return Conflict(new { message = "Employee ID already exists" });

            if (!string.IsNullOrWhiteSpace(request.Email) &&
                await _context.Employees.AnyAsync(e => e.Email == request.Email))
                return Conflict(new { message = "Email already in use" });

            // Generate password from EmpCode
            var generatedPassword = GeneratePasswordFromEmpCode(request.EmpCode);

            // Create Employee entity
            var employee = new Employee
            {
                EmpCode = request.EmpCode,
                FirstName = request.FirstName,
                MiddleName = request.MiddleName,
                LastName = request.LastName,
                Department = request.Department,
                Position = request.Position,
                Email = request.Email,
                Phone = request.Phone,
                Role = request.Role,
                PasswordHash = _passwordHasher.HashPassword(generatedPassword),  // Hash the generated password
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // Return response with generated password for admin
            var response = new
            {
                employee.EmployeeID,
                employee.EmpCode,
                employee.FirstName,
                employee.MiddleName,
                employee.LastName,
                employee.Department,
                employee.Position,
                employee.Email,
                employee.Phone,
                employee.Role,
                employee.IsActive,
                GeneratedPassword = generatedPassword,  // Include for admin reference
                message = "Employee created successfully. Please note the generated password."
            };

            return CreatedAtAction(nameof(GetEmployee), new { empCode = employee.EmpCode }, response);
        }

        // Request DTO for Update
        public class UpdateEmployeeRequest
        {
            public string FirstName { get; set; }
            public string? MiddleName { get; set; }
            public string LastName { get; set; }
            public string? Department { get; set; }
            public string? Position { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? Role { get; set; }
        }

        [HttpPut("{empCode}")]
        public async Task<IActionResult> UpdateEmployee(string empCode, [FromBody] UpdateEmployeeRequest request)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmpCode == empCode);
            if (employee == null) return NotFound();

            employee.FirstName = request.FirstName;
            employee.MiddleName = request.MiddleName;
            employee.LastName = request.LastName;
            employee.Department = request.Department;
            employee.Position = request.Position;
            employee.Email = request.Email;
            employee.Phone = request.Phone;
            employee.Role = request.Role;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Updated successfully" });
        }

        // Reset Password endpoint
        [HttpPost("{empCode}/reset-password")]
        public async Task<IActionResult> ResetPassword(string empCode)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmpCode == empCode);
            if (employee == null) return NotFound();

            // Generate new password
            var newPassword = GeneratePasswordFromEmpCode(empCode);
            employee.PasswordHash = _passwordHasher.HashPassword(newPassword);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Password reset successfully",
                NewPassword = newPassword
            });
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