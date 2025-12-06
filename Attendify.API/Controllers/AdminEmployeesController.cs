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
    [FromQuery] string? department = null,
    [FromQuery] string? position = null,
    [FromQuery] string? role = null,
    [FromQuery] string? sortBy = "EmpCode",
    [FromQuery] string? sortOrder = "asc",
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50)
        {
            var query = _context.Employees.AsQueryable();

            // Search using EmpCode (string) — NEVER EmployeeID (int)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(e =>
                    e.EmpCode.ToLower().Contains(term) ||
                    e.FirstName.ToLower().Contains(term) ||
                    e.LastName.ToLower().Contains(term) ||
                    (e.Email != null && e.Email.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(department) && department != "All Departments")
                query = query.Where(e => e.Department == department);

            if (!string.IsNullOrWhiteSpace(position) && position != "All Position")
                query = query.Where(e => e.Position == position);

            if (!string.IsNullOrWhiteSpace(role) && role != "All Roles")
                query = query.Where(e => e.Role == role);

            // Sort by string EmpCode
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
            if (string.IsNullOrWhiteSpace(employee.EmpCode))
                return BadRequest(new { message = "Employee ID (EmpCode) is required" });

            if (await _context.Employees.AnyAsync(e => e.EmpCode == employee.EmpCode))
                return Conflict(new { message = "Employee ID already exists" });

            if (!string.IsNullOrWhiteSpace(employee.Email) &&
                await _context.Employees.AnyAsync(e => e.Email == employee.Email))
                return Conflict(new { message = "Email already in use" });

            employee.CreatedAt = DateTime.UtcNow;
            employee.IsActive = true;

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEmployee), new { empCode = employee.EmpCode }, employee);
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