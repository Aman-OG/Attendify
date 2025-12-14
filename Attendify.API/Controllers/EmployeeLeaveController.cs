using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Attendify.DATA;
using Attendify.DATA.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Attendify.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LeavesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LeavesController(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // Nested DTOs
        public class LeaveDTO
        {
            public int LeaveID { get; set; }
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
            public string? ReasonTitle { get; set; }
            public string? Detail { get; set; }
            public string? Status { get; set; }
            public string? AdminResponse { get; set; }
            public DateTime CreatedAt { get; set; }
            public bool CanCancel { get; set; }
            public string StatusColor
            {
                get
                {
                    return Status?.ToLower() switch
                    {
                        "pending" => "#FF9800",
                        "approved" => "#38b000",
                        "rejected" => "#FF6B6B",
                        _ => "#888888"
                    };
                }
            }
        }

        public class LeaveRequestDTO
        {
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public string ReasonTitle { get; set; } = string.Empty;
            public string? Detail { get; set; }
        }

        public class LeaveStatsDTO
        {
            public int PendingCount { get; set; }
            public int ApprovedCount { get; set; }
            public int RejectedCount { get; set; }
        }

        // Helper method to get current employee code
        private string GetCurrentEmployeeCode()
        {
            // This assumes you're using JWT or similar authentication
            // Adjust based on your authentication setup
            var user = _httpContextAccessor.HttpContext?.User;
            var empCodeClaim = user?.FindFirst("EmployeeCode") ??
                              user?.FindFirst("sub") ??
                              user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

            return empCodeClaim?.Value ?? throw new UnauthorizedAccessException("Employee not authenticated");
        }

        // GET: api/Leaves/my-leaves
        [HttpGet("my-leaves")]
        public async Task<ActionResult<IEnumerable<LeaveDTO>>> GetMyLeaves()
        {
            try
            {
                var empCode = GetCurrentEmployeeCode();

                var leaves = await _context.Leaves
                    .Where(l => l.EmpCode == empCode)
                    .OrderByDescending(l => l.CreatedAt)
                    .Select(l => new LeaveDTO
                    {
                        LeaveID = l.LeaveID,
                        FromDate = l.FromDate,
                        ToDate = l.ToDate,
                        ReasonTitle = l.ReasonTitle,
                        Detail = l.Detail,
                        Status = l.Status,
                        AdminResponse = l.AdminResponse,
                        CreatedAt = l.CreatedAt,
                        CanCancel = l.Status == "pending" && l.FromDate > DateTime.UtcNow.AddDays(1)
                    })
                    .ToListAsync();

                return Ok(leaves);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching leaves", error = ex.Message });
            }
        }

        // GET: api/Leaves/stats
        [HttpGet("stats")]
        public async Task<ActionResult<LeaveStatsDTO>> GetLeaveStats()
        {
            try
            {
                var empCode = GetCurrentEmployeeCode();

                var stats = new LeaveStatsDTO
                {
                    PendingCount = await _context.Leaves
                        .CountAsync(l => l.EmpCode == empCode && l.Status == "pending"),
                    ApprovedCount = await _context.Leaves
                        .CountAsync(l => l.EmpCode == empCode && l.Status == "approved"),
                    RejectedCount = await _context.Leaves
                        .CountAsync(l => l.EmpCode == empCode && l.Status == "rejected")
                };

                return Ok(stats);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching stats", error = ex.Message });
            }
        }

        // GET: api/Leaves/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LeaveDTO>> GetLeave(int id)
        {
            try
            {
                var empCode = GetCurrentEmployeeCode();

                var leave = await _context.Leaves
                    .Where(l => l.LeaveID == id && l.EmpCode == empCode)
                    .Select(l => new LeaveDTO
                    {
                        LeaveID = l.LeaveID,
                        FromDate = l.FromDate,
                        ToDate = l.ToDate,
                        ReasonTitle = l.ReasonTitle,
                        Detail = l.Detail,
                        Status = l.Status,
                        AdminResponse = l.AdminResponse,
                        CreatedAt = l.CreatedAt,
                        CanCancel = l.Status == "pending" && l.FromDate > DateTime.UtcNow.AddDays(1)
                    })
                    .FirstOrDefaultAsync();

                if (leave == null)
                {
                    return NotFound(new { message = "Leave request not found" });
                }

                return Ok(leave);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching leave details", error = ex.Message });
            }
        }

        // POST: api/Leaves
        [HttpPost]
        public async Task<ActionResult<LeaveDTO>> CreateLeave(LeaveRequestDTO leaveRequest)
        {
            try
            {
                var empCode = GetCurrentEmployeeCode();

                // Validation
                if (leaveRequest.FromDate.Date < DateTime.UtcNow.Date)
                {
                    return BadRequest(new { message = "From date cannot be in the past" });
                }

                if (leaveRequest.ToDate < leaveRequest.FromDate)
                {
                    return BadRequest(new { message = "To date must be after from date" });
                }

                if (string.IsNullOrWhiteSpace(leaveRequest.ReasonTitle))
                {
                    return BadRequest(new { message = "Reason title is required" });
                }

                // Check for overlapping leave requests
                var overlappingLeaves = await _context.Leaves
                    .Where(l => l.EmpCode == empCode &&
                                l.Status != "rejected" &&
                                ((l.FromDate <= leaveRequest.ToDate && l.ToDate >= leaveRequest.FromDate)))
                    .ToListAsync();

                if (overlappingLeaves.Any())
                {
                    return BadRequest(new { message = "You already have a leave request for these dates" });
                }

                var leave = new Leave
                {
                    EmpCode = empCode,
                    FromDate = leaveRequest.FromDate.Date,
                    ToDate = leaveRequest.ToDate.Date,
                    ReasonTitle = leaveRequest.ReasonTitle.Trim(),
                    Detail = leaveRequest.Detail?.Trim(),
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Leaves.Add(leave);
                await _context.SaveChangesAsync();

                var leaveDto = new LeaveDTO
                {
                    LeaveID = leave.LeaveID,
                    FromDate = leave.FromDate,
                    ToDate = leave.ToDate,
                    ReasonTitle = leave.ReasonTitle,
                    Detail = leave.Detail,
                    Status = leave.Status,
                    AdminResponse = leave.AdminResponse,
                    CreatedAt = leave.CreatedAt,
                    CanCancel = true
                };

                return CreatedAtAction(nameof(GetLeave), new { id = leave.LeaveID }, leaveDto);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating leave request", error = ex.Message });
            }
        }

        // DELETE: api/Leaves/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelLeave(int id)
        {
            try
            {
                var empCode = GetCurrentEmployeeCode();

                var leave = await _context.Leaves
                    .FirstOrDefaultAsync(l => l.LeaveID == id && l.EmpCode == empCode);

                if (leave == null)
                {
                    return NotFound(new { message = "Leave request not found" });
                }

                if (leave.Status != "pending")
                {
                    return BadRequest(new { message = "Only pending leave requests can be cancelled" });
                }

                if (leave.FromDate <= DateTime.UtcNow.AddDays(1))
                {
                    return BadRequest(new { message = "Leave request cannot be cancelled within 24 hours of start date" });
                }

                _context.Leaves.Remove(leave);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Leave request cancelled successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while cancelling leave request", error = ex.Message });
            }
        }

        // GET: api/Leaves/remaining-days
        [HttpGet("remaining-days")]
        public async Task<ActionResult<object>> GetRemainingLeaveDays()
        {
            try
            {
                var empCode = GetCurrentEmployeeCode();

                // Get current year
                var currentYear = DateTime.UtcNow.Year;

                // Get employee's approved leaves for current year
                var approvedLeaves = await _context.Leaves
                    .Where(l => l.EmpCode == empCode &&
                                l.Status == "approved" &&
                                l.FromDate.HasValue &&
                                l.ToDate.HasValue &&
                                l.FromDate.Value.Year == currentYear)
                    .ToListAsync();

                // Calculate total approved leave days
                int totalLeaveDays = 0;
                foreach (var leave in approvedLeaves)
                {
                    if (leave.FromDate.HasValue && leave.ToDate.HasValue)
                    {
                        // Add 1 to include both start and end dates
                        totalLeaveDays += (int)(leave.ToDate.Value.Date - leave.FromDate.Value.Date).TotalDays + 1;
                    }
                }

                // Get total leave allowance from employee or company policy
                // This is a placeholder - adjust based on your business logic
                int annualLeaveAllowance = 20; // Default 20 days per year

                return Ok(new
                {
                    remainingDays = annualLeaveAllowance - totalLeaveDays,
                    usedDays = totalLeaveDays,
                    annualAllowance = annualLeaveAllowance
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while calculating remaining days", error = ex.Message });
            }
        }
    }
}