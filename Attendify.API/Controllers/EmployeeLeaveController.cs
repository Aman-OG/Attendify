using Attendify.DATA;
using Attendify.DATA.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Attendify.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeLeaveController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EmployeeLeaveController> _logger;

        public EmployeeLeaveController(AppDbContext context, ILogger<EmployeeLeaveController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // DTOs nested inside controller
        public class LeaveRequestDto
        {
            public string EmpCode { get; set; } = null!;
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public string ReasonTitle { get; set; } = null!;
            public string? DetailedReason { get; set; }
        }

        public class UpdateLeaveStatusDto
        {
            public int LeaveId { get; set; }
            public string Status { get; set; } = null!;
            public string? AdminResponse { get; set; }
        }

        public class CancelLeaveDto
        {
            public int LeaveId { get; set; }
        }

        public class LeaveResponseDto
        {
            public int LeaveId { get; set; }
            public string FromDate { get; set; } = null!;
            public string ToDate { get; set; } = null!;
            public string ReasonTitle { get; set; } = null!;
            public string? DetailedReason { get; set; }
            public string Status { get; set; } = null!;
            public string? AdminResponse { get; set; }
            public string CreatedAt { get; set; } = null!;
            public bool CanCancel { get; set; }
            public string StatusColor { get; set; } = "#FF9800"; // Default to pending
        }

        public class LeaveStatsDto
        {
            public int Pending { get; set; }
            public int Approved { get; set; }
            public int Rejected { get; set; }
        }

        public class ApiResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = null!;
            public object? Data { get; set; }
        }

        [HttpGet("requests/{empCode}")]
        public async Task<ActionResult<ApiResponseDto>> GetLeaveRequests(string empCode)
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

                var leaves = await _context.Leaves
                    .Where(l => l.EmpCode == empCode)
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(50)
                    .ToListAsync();

                var leaveResponses = leaves.Select(l => new LeaveResponseDto
                {
                    LeaveId = l.LeaveID,
                    FromDate = l.FromDate.HasValue ? l.FromDate.Value.ToString("yyyy-MM-dd") : "N/A",
                    ToDate = l.ToDate.HasValue ? l.ToDate.Value.ToString("yyyy-MM-dd") : "N/A",
                    ReasonTitle = l.ReasonTitle ?? "No reason provided",
                    DetailedReason = l.Detail,
                    Status = l.Status ?? "Pending",
                    AdminResponse = l.AdminResponse,
                    CreatedAt = l.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    CanCancel = l.Status == "Pending" && l.CreatedAt > DateTime.UtcNow.AddDays(-1),
                    StatusColor = GetStatusColor(l.Status ?? "Pending")
                }).ToList();

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Leave requests retrieved successfully",
                    Data = leaveResponses
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leave requests for employee: {EmpCode}", empCode);
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = $"An error occurred while fetching leave requests: {ex.Message}"
                });
            }
        }

        [HttpGet("stats/{empCode}")]
        public async Task<ActionResult<ApiResponseDto>> GetLeaveStats(string empCode)
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

                var pendingCount = await _context.Leaves
                    .CountAsync(l => l.EmpCode == empCode && (l.Status == null || l.Status == "Pending"));

                var approvedCount = await _context.Leaves
                    .CountAsync(l => l.EmpCode == empCode && l.Status == "Approved");

                var rejectedCount = await _context.Leaves
                    .CountAsync(l => l.EmpCode == empCode && l.Status == "Rejected");

                var response = new LeaveStatsDto
                {
                    Pending = pendingCount,
                    Approved = approvedCount,
                    Rejected = rejectedCount
                };

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Leave stats retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leave stats for employee: {EmpCode}", empCode);
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = $"An error occurred while fetching leave statistics: {ex.Message}"
                });
            }
        }

        [HttpPost("request")]
        public async Task<ActionResult<ApiResponseDto>> RequestLeave([FromBody] LeaveRequestDto request)
        {
            try
            {
                _logger.LogInformation("RequestLeave called with EmpCode: {EmpCode}", request.EmpCode);

                // Validate request
                if (string.IsNullOrEmpty(request.EmpCode))
                {
                    return BadRequest(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Employee code is required"
                    });
                }

                if (string.IsNullOrEmpty(request.ReasonTitle))
                {
                    return BadRequest(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Reason title is required"
                    });
                }

                // Validate dates
                if (request.FromDate > request.ToDate)
                {
                    return BadRequest(new ApiResponseDto
                    {
                        Success = false,
                        Message = "From date cannot be after To date"
                    });
                }

                // Check if dates are in the past
                if (request.FromDate.Date < DateTime.UtcNow.Date)
                {
                    return BadRequest(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Leave cannot be requested for past dates"
                    });
                }

                // Ensure dates are in UTC
                var fromDateUtc = DateTime.SpecifyKind(request.FromDate.Date, DateTimeKind.Utc);
                var toDateUtc = DateTime.SpecifyKind(request.ToDate.Date, DateTimeKind.Utc);

                _logger.LogInformation("Checking for overlapping leaves from {FromDate} to {ToDate}", fromDateUtc, toDateUtc);

                // Check for overlapping leave requests (exclude rejected ones)
                var overlappingLeaves = await _context.Leaves
                    .Where(l => l.EmpCode == request.EmpCode &&
                               l.Status != "Rejected" &&
                               l.Status != "Cancelled" &&
                               l.FromDate.HasValue &&
                               l.ToDate.HasValue)
                    .Where(l => (l.FromDate <= toDateUtc && l.ToDate >= fromDateUtc))
                    .ToListAsync();

                if (overlappingLeaves.Any())
                {
                    return BadRequest(new ApiResponseDto
                    {
                        Success = false,
                        Message = "You already have a pending or approved leave request for these dates"
                    });
                }

                // Create new leave request
                var leave = new Leave
                {
                    EmpCode = request.EmpCode,
                    FromDate = fromDateUtc,
                    ToDate = toDateUtc,
                    ReasonTitle = request.ReasonTitle,
                    Detail = request.DetailedReason,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Adding leave request to database...");
                _context.Leaves.Add(leave);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Leave request saved with ID: {LeaveId}", leave.LeaveID);

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Leave request submitted successfully",
                    Data = new
                    {
                        LeaveId = leave.LeaveID,
                        FromDate = leave.FromDate?.ToString("yyyy-MM-dd"),
                        ToDate = leave.ToDate?.ToString("yyyy-MM-dd"),
                        ReasonTitle = leave.ReasonTitle
                    }
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while submitting leave request for employee: {EmpCode}", request.EmpCode);
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting leave request for employee: {EmpCode}", request.EmpCode);
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = $"An error occurred while submitting leave request: {ex.Message}"
                });
            }
        }

        [HttpPut("cancel")]
        public async Task<ActionResult<ApiResponseDto>> CancelLeave([FromBody] CancelLeaveDto request)
        {
            try
            {
                var leave = await _context.Leaves
                    .FirstOrDefaultAsync(l => l.LeaveID == request.LeaveId);

                if (leave == null)
                {
                    return NotFound(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Leave request not found"
                    });
                }

                // Check if leave can be cancelled (only pending requests within 24 hours)
                if (leave.Status != "Pending")
                {
                    return BadRequest(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Only pending leave requests can be cancelled"
                    });
                }

                if (leave.CreatedAt < DateTime.UtcNow.AddDays(-1))
                {
                    return BadRequest(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Leave requests can only be cancelled within 24 hours of submission"
                    });
                }

                // Update status
                leave.Status = "Cancelled";
                leave.AdminResponse = "Cancelled by employee";

                await _context.SaveChangesAsync();

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Leave request cancelled successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling leave request ID: {LeaveId}", request.LeaveId);
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = $"An error occurred while cancelling leave request: {ex.Message}"
                });
            }
        }

        [HttpGet("details/{leaveId}")]
        public async Task<ActionResult<ApiResponseDto>> GetLeaveDetails(int leaveId)
        {
            try
            {
                var leave = await _context.Leaves
                    .FirstOrDefaultAsync(l => l.LeaveID == leaveId);

                if (leave == null)
                {
                    return NotFound(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Leave request not found"
                    });
                }

                var leaveResponse = new LeaveResponseDto
                {
                    LeaveId = leave.LeaveID,
                    FromDate = leave.FromDate.HasValue ? leave.FromDate.Value.ToString("yyyy-MM-dd") : "N/A",
                    ToDate = leave.ToDate.HasValue ? leave.ToDate.Value.ToString("yyyy-MM-dd") : "N/A",
                    ReasonTitle = leave.ReasonTitle ?? "No reason provided",
                    DetailedReason = leave.Detail,
                    Status = leave.Status ?? "Pending",
                    AdminResponse = leave.AdminResponse,
                    CreatedAt = leave.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    CanCancel = leave.Status == "Pending" && leave.CreatedAt > DateTime.UtcNow.AddDays(-1),
                    StatusColor = GetStatusColor(leave.Status ?? "Pending")
                };

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Leave details retrieved successfully",
                    Data = leaveResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leave details for ID: {LeaveId}", leaveId);
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = $"An error occurred while fetching leave details: {ex.Message}"
                });
            }
        }

        // Make this method static to fix EF translation issue
        private static string GetStatusColor(string status)
        {
            return status.ToLower() switch
            {
                "approved" => "#38b000",  // Green
                "rejected" => "#FF6B6B",  // Red
                "cancelled" => "#666666", // Gray
                _ => "#FF9800"            // Orange for pending
            };
        }
    }
}