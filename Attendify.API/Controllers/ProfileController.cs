using Attendify.DATA;
using Attendify.DATA.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Attendify.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(AppDbContext context, ILogger<ProfileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // DTO classes nested in controller
        public class EmployeeProfileDto
        {
            public int EmployeeID { get; set; }
            public string EmpCode { get; set; } = null!;
            public string FirstName { get; set; } = null!;
            public string? MiddleName { get; set; }
            public string LastName { get; set; } = null!;
            public string? Department { get; set; }
            public string? Position { get; set; }
            public string? Email { get; set; }
            public string Role { get; set; } = null!;
            public string? Phone { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? LastPasswordChange { get; set; }
        }

        public class ApiResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = null!;
            public EmployeeProfileDto? Data { get; set; }
        }

        [HttpGet("{empCode}")]
        public async Task<ActionResult<ApiResponseDto>> GetProfile(string empCode)
        {
            try
            {
                if (string.IsNullOrEmpty(empCode))
                {
                    return BadRequest(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Employee code is required"
                    });
                }

                var employee = await _context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmpCode == empCode);

                if (employee == null)
                {
                    return NotFound(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Employee not found"
                    });
                }

                var profileDto = new EmployeeProfileDto
                {
                    EmployeeID = employee.EmployeeID,
                    EmpCode = employee.EmpCode,
                    FirstName = employee.FirstName,
                    MiddleName = employee.MiddleName,
                    LastName = employee.LastName,
                    Department = employee.Department,
                    Position = employee.Position,
                    Email = employee.Email,
                    Role = employee.Role,
                    Phone = employee.Phone,
                    IsActive = employee.IsActive,
                    CreatedAt = employee.CreatedAt,
                    LastPasswordChange = employee.LastPasswordChange
                };

                _logger.LogInformation("Profile retrieved for employee: {EmpCode}", empCode);

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Profile retrieved successfully",
                    Data = profileDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profile for employee: {EmpCode}", empCode);
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "An error occurred while retrieving profile"
                });
            }
        }

        [HttpPut("{empCode}")]
        public async Task<ActionResult<ApiResponseDto>> UpdateProfile(string empCode, [FromBody] UpdateProfileDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(empCode))
                {
                    return BadRequest(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Employee code is required"
                    });
                }

                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmpCode == empCode);

                if (employee == null)
                {
                    return NotFound(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Employee not found"
                    });
                }

                // Update allowed fields
                if (!string.IsNullOrEmpty(request.FirstName))
                    employee.FirstName = request.FirstName;

                if (request.MiddleName != null)
                    employee.MiddleName = request.MiddleName;

                if (!string.IsNullOrEmpty(request.LastName))
                    employee.LastName = request.LastName;

                if (!string.IsNullOrEmpty(request.Email))
                    employee.Email = request.Email;

                if (!string.IsNullOrEmpty(request.Phone))
                    employee.Phone = request.Phone;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Profile updated for employee: {EmpCode}", empCode);

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Profile updated successfully"
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error updating profile for employee: {EmpCode}", empCode);
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "Database error occurred"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for employee: {EmpCode}", empCode);
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "An error occurred while updating profile"
                });
            }
        }

        // Update DTO
        public class UpdateProfileDto
        {
            public string? FirstName { get; set; }
            public string? MiddleName { get; set; }
            public string? LastName { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
        }
    }
}