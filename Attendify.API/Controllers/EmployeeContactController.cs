using Attendify.DATA;
using Attendify.DATA.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendify.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeContactController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EmployeeContactController> _logger;

        public EmployeeContactController(AppDbContext context, ILogger<EmployeeContactController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // DTOs
        public class ContactRequestDto
        {
            public string EmpCode { get; set; } = null!;
            public string Date { get; set; } = null!;
            public string Type { get; set; } = null!;
            public string Message { get; set; } = null!;
        }

        public class ContactResponseDto
        {
            public int RequestId { get; set; }
            public string Date { get; set; } = null!;
            public string Type { get; set; } = null!;
            public string Message { get; set; } = null!;
            public string Status { get; set; } = null!;
            public string? AdminReply { get; set; }
            public string CreatedAt { get; set; } = null!;
            public string StatusColor { get; set; } = "#FF9800";
            public string TypeIcon { get; set; } = "📝";
        }

        public class ContactStatsDto
        {
            public int Total { get; set; }
            public int Pending { get; set; }
            public int Resolved { get; set; }
        }

        public class ApiResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = null!;
            public object? Data { get; set; }
        }

        [HttpGet("requests/{empCode}")]
        public async Task<ActionResult<ApiResponseDto>> GetContactRequests(string empCode)
        {
            try
            {
                var requests = await _context.EmployeeRequests
                    .Where(r => r.EmpCode == empCode)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(30)
                    .Select(r => new ContactResponseDto
                    {
                        RequestId = r.RequestID,
                        Date = r.Date ?? "N/A",
                        Type = r.Type,
                        Message = r.Message,
                        Status = r.Status,
                        AdminReply = r.AdminReply,
                        CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        StatusColor = GetStatusColor(r.Status),
                        TypeIcon = GetTypeIcon(r.Type)
                    })
                    .ToListAsync();

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Contact requests retrieved successfully",
                    Data = requests
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contact requests for employee: {EmpCode}", empCode);
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "An error occurred while fetching contact requests"
                });
            }
        }

        [HttpGet("stats/{empCode}")]
        public async Task<ActionResult<ApiResponseDto>> GetContactStats(string empCode)
        {
            try
            {
                var total = await _context.EmployeeRequests
                    .CountAsync(r => r.EmpCode == empCode);

                var pending = await _context.EmployeeRequests
                    .CountAsync(r => r.EmpCode == empCode && r.Status == "Pending");

                var resolved = await _context.EmployeeRequests
                    .CountAsync(r => r.EmpCode == empCode && (r.Status == "Approved" || r.Status == "Rejected"));

                var stats = new ContactStatsDto
                {
                    Total = total,
                    Pending = pending,
                    Resolved = resolved
                };

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Contact stats retrieved successfully",
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contact stats for employee: {EmpCode}", empCode);
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "An error occurred while fetching contact statistics"
                });
            }
        }

        [HttpPost("request")]
        public async Task<ActionResult<ApiResponseDto>> SubmitContactRequest([FromBody] ContactRequestDto request)
        {
            try
            {
                // Validate request
                // EmpCode can be null for Guest requests
                string? finalEmpCode = request.EmpCode;
                if (string.IsNullOrEmpty(finalEmpCode) || finalEmpCode == "UNKNOWN")
                {
                    finalEmpCode = null;
                }

                if (string.IsNullOrEmpty(request.Type))
                {
                    return BadRequest(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Request type is required"
                    });
                }

                if (string.IsNullOrEmpty(request.Message))
                {
                    return BadRequest(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Message is required"
                    });
                }

                // Create new request
                var employeeRequest = new EmployeeRequest
                {
                    EmpCode = finalEmpCode,
                    Date = request.Date,
                    Type = request.Type,
                    Message = request.Message,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.EmployeeRequests.Add(employeeRequest);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Request submitted successfully. Admin will respond soon.",
                    Data = new
                    {
                        RequestId = employeeRequest.RequestID,
                        Date = employeeRequest.Date,
                        Type = employeeRequest.Type
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting contact request for employee: {EmpCode}", request.EmpCode);
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "An error occurred while submitting request"
                });
            }
        }

        [HttpGet("types")]
        public ActionResult<ApiResponseDto> GetRequestTypes()
        {
            try
            {
                var types = new List<object>
                {
                    new { Value = "Late", Label = "Late Arrival", Icon = "⏰" },
                    new { Value = "Absence", Label = "Absence Report", Icon = "🚫" },
                    new { Value = "Correction", Label = "Attendance Correction", Icon = "✏️" },
                    new { Value = "Other", Label = "Other Inquiry", Icon = "📝" }
                };

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Request types retrieved successfully",
                    Data = types
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting request types");
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "An error occurred while fetching request types"
                });
            }
        }

        private static string GetStatusColor(string status)
        {
            return status.ToLower() switch
            {
                "approved" => "#38b000",  // Green
                "rejected" => "#FF6B6B",  // Red
                _ => "#FF9800"            // Orange for pending
            };
        }

        private static string GetTypeIcon(string type)
        {
            return type.ToLower() switch
            {
                "late" => "⏰",
                "absence" => "🚫",
                "correction" => "✏️",
                _ => "📝"
            };
        }
    }
}