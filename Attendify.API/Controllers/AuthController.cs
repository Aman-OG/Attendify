using Attendify.DATA;
using Attendify.DATA.Models;
using Attendify.API.Services;  
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Attendify.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

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
            public string? MiddleName { get; set; }
            public string LastName { get; set; } = null!;
            public string? Department { get; set; }
            public string? Position { get; set; }
            public string Email { get; set; } = null!;
            public string Role { get; set; } = null!;
        }




        private readonly AppDbContext _context;
        private readonly ILogger<AuthController> _logger;
        private readonly IPasswordHasher _passwordHasher;  // Add this

        public AuthController(
            AppDbContext context,
            ILogger<AuthController> logger,
            IPasswordHasher passwordHasher)  // Add parameter
        {
            _context = context;
            _logger = logger;
            _passwordHasher = passwordHasher;
        }

        // ... [rest of your DTO classes remain the same]

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return Ok(new LoginResponse
                    {
                        Success = false,
                        Message = "Email and password are required"
                    });
                }

                // Find employee by email
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == request.Email && e.IsActive);

                if (employee == null)
                {
                    return Ok(new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    });
                }

                // Verify password using BCrypt
                if (!_passwordHasher.VerifyPassword(employee.PasswordHash, request.Password))
                {
                    return Ok(new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    });
                }

                // Return success with employee data
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
                        MiddleName = employee.MiddleName,
                        LastName = employee.LastName,
                        Department = employee.Department,
                        Position = employee.Position,
                        Email = employee.Email!,
                        Role = employee.Role
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "An error occurred during login"
                });
            }
        }

        // REMOVE the old HashPassword method since we're using IPasswordHasher now
        // private string HashPassword(string password)
        // {
        //     using (var sha256 = SHA256.Create())
        //     {
        //         var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        //         return Convert.ToBase64String(bytes);
        //     }
        // }
    }
}