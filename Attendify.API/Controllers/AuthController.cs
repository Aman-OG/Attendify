using Attendify.DATA;
using Attendify.DATA.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendify.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext context, ILogger<AuthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ===================== DTOs =====================

        public class LoginRequest
        {
            public string Email { get; set; } = null!;
            public string Password { get; set; } = null!;
        }

        public class LoginResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = null!;
            public EmployeeData? Employee { get; set; }
            public string? Role { get; set; }
        }

        public class EmployeeData
        {
            public int EmployeeID { get; set; }
            public string EmpCode { get; set; } = null!;
            public string FirstName { get; set; } = null!;
            public string LastName { get; set; } = null!;
            public string Email { get; set; } = null!;
            public string Role { get; set; } = null!;
            public string? Department { get; set; }
            public string? Position { get; set; }
            public string? MiddleName { get; set; }
        }

        // ===================== LOGIN =====================

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    return Ok(new LoginResponse
                    {
                        Success = false,
                        Message = "Email and password are required"
                    });
                }

                string email = request.Email.Trim().ToLower();
                string password = request.Password;

                // =====================================================
                // 🔥 TEMP HARDCODED LOGIN (FOR TEAM DEMO ONLY)
                // =====================================================

                if (email == "admin" && password == "1234")
                {
                    return Ok(new LoginResponse
                    {
                        Success = true,
                        Message = "Hardcoded admin login",
                        Role = "Admin",
                        Employee = new EmployeeData
                        {
                            EmployeeID = 0,
                            EmpCode = "ADMIN001",
                            FirstName = "System",
                            LastName = "Admin",
                            Email = "admin",
                            Role = "Admin",
                            Department = "IT",
                            Position = "Administrator"
                        }
                    });
                }

                if (email == "employee" && password == "1234")
                {
                    return Ok(new LoginResponse
                    {
                        Success = true,
                        Message = "Hardcoded employee login",
                        Role = "Employee",
                        Employee = new EmployeeData
                        {
                            EmployeeID = 0,
                            EmpCode = "EMP001",
                            FirstName = "Demo",
                            LastName = "Employee",
                            Email = "employee",
                            Role = "Employee",
                            Department = "General",
                            Position = "Staff"
                        }
                    });
                }

                // =====================================================
                // ⬇️ NORMAL DATABASE LOGIN (TEMPORARILY DISABLED)
                // =====================================================

                /*
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email.ToLower() == email && e.IsActive);

                if (employee == null)
                {
                    return Ok(new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    });
                }

                if (!BCrypt.Net.BCrypt.Verify(password, employee.PasswordHash))
                {
                    return Ok(new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    });
                }

                return Ok(new LoginResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Role = employee.Role,
                    Employee = new EmployeeData
                    {
                        EmployeeID = employee.EmployeeID,
                        EmpCode = employee.EmpCode,
                        FirstName = employee.FirstName,
                        LastName = employee.LastName,
                        MiddleName = employee.MiddleName,
                        Email = employee.Email!,
                        Role = employee.Role,
                        Department = employee.Department,
                        Position = employee.Position
                    }
                });
                */

                // =====================================================
                // FALLBACK
                // =====================================================

                return Ok(new LoginResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "Server error during login"
                });
            }
        }
    }
}
