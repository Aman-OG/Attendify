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
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private readonly IPasswordHasher _passwordHasher;

        public AccountController(
            AppDbContext context,
            ILogger<AccountController> logger,
            IPasswordHasher passwordHasher  
        )
        {
            _context = context;
            _logger = logger;
            _passwordHasher = passwordHasher;
        }


        // DTOs
        public class ChangePasswordDto
        {
            public string EmpCode { get; set; } = null!;
            public string CurrentPassword { get; set; } = null!;
            public string NewPassword { get; set; } = null!;
            public string ConfirmPassword { get; set; } = null!;
        }

        public class ApiResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = null!;
            public object? Data { get; set; }
        }

        [HttpPost("change-password")]
        public async Task<ActionResult<ApiResponseDto>> ChangePassword([FromBody] ChangePasswordDto request)
        {
            try
            {
                // Validate request model first
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Data = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                // Validate request fields
                if (string.IsNullOrEmpty(request.EmpCode))
                {
                    return BadRequest(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Employee code is required"
                    });
                }



                // Find employee
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmpCode == request.EmpCode);

                if (employee == null)
                {
                    return BadRequest(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Employee not found"
                    });
                }

                // Check if PasswordHash field exists in Employee model
                // If not, you need to add it to your Employee model
                if (string.IsNullOrEmpty(employee.PasswordHash))
                {
                    // For new users without a password, create one
                    // Or handle as appropriate for your application
                    return BadRequest(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Password not set for this user. Please contact administrator."
                    });
                }

                // Verify current password
                if (!_passwordHasher.VerifyPassword(employee.PasswordHash, request.CurrentPassword))
                {
                    return BadRequest(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Current password is incorrect"
                    });
                }

                // Hash and save new password
                employee.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password changed successfully for employee: {EmpCode}", request.EmpCode);

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Password changed successfully"
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error changing password");
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "Database error occurred"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for employee: {EmpCode}", request.EmpCode);
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "An error occurred while changing password"
                });
            }
        }
    }
}