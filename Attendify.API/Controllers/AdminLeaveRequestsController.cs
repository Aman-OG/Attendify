using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Attendify.DATA;
using Attendify.DATA.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Attendify.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaveRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LeaveRequestsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/LeaveRequests
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetLeaveRequests(
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] string? filter = "all")
        {
            try
            {
                var query = _context.Leaves
                    .Include(l => l.Employee)
                    .AsQueryable();

                // Apply status filter
                if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
                {
                    query = query.Where(l => l.Status == status);
                }

                // Apply filter (today, etc.)
                if (filter.ToLower() == "today")
                {
                    var today = DateTime.UtcNow.Date;
                    query = query.Where(l => l.FromDate.HasValue && l.FromDate.Value.Date <= today &&
                                            l.ToDate.HasValue && l.ToDate.Value.Date >= today);
                }

                // Apply search
                if (!string.IsNullOrEmpty(search))
                {
                    var term = search.ToLower();
                    query = query.Where(l =>
                        (l.Employee.FirstName + " " + l.Employee.LastName).ToLower().Contains(term) ||
                        l.Employee.EmpCode.ToLower().Contains(term) ||
                        (l.ReasonTitle ?? "").ToLower().Contains(term) ||
                        (l.Employee.Department ?? "").ToLower().Contains(term));
                }

                var leaves = await query
                    .Select(l => new
                    {
                        LeaveId = l.LeaveID,
                        No = l.LeaveID.ToString(),
                        EmployeeName = l.Employee.FirstName + " " + l.Employee.LastName,
                        EmpId = l.Employee.EmpCode,
                        Department = l.Employee.Department ?? "N/A",
                        Position = l.Employee.Position ?? "N/A",
                        Email = l.Employee.Email ?? "N/A",
                        FromDate = l.FromDate.HasValue ? l.FromDate.Value.ToString("dd/MM/yy") : "N/A",
                        ToDate = l.ToDate.HasValue ? l.ToDate.Value.ToString("dd/MM/yy") : "N/A",
                        Reason = l.ReasonTitle ?? "No reason provided",
                        Description = l.Detail ?? "No additional details",
                        Status = l.Status ?? "Pending",
                        StatusColor = (l.Status ?? "Pending") == "Approved" ? "#2FBF4C" :
                                     (l.Status ?? "Pending") == "Rejected" ? "#D23C3C" : "#E3C63A"
                    })
                    .ToListAsync();

                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/LeaveRequests/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetLeaveRequest(int id)
        {
            try
            {
                var leave = await _context.Leaves
                    .Include(l => l.Employee)
                    .Where(l => l.LeaveID == id)
                    .Select(l => new
                    {
                        LeaveId = l.LeaveID,
                        No = l.LeaveID.ToString(),
                        EmployeeName = l.Employee.FirstName + " " + l.Employee.LastName,
                        EmpId = l.Employee.EmpCode,
                        Department = l.Employee.Department ?? "N/A",
                        Position = l.Employee.Position ?? "N/A",
                        Email = l.Employee.Email ?? "N/A",
                        FromDate = l.FromDate.HasValue ? l.FromDate.Value.ToString("dd/MM/yy") : "N/A",
                        ToDate = l.ToDate.HasValue ? l.ToDate.Value.ToString("dd/MM/yy") : "N/A",
                        Reason = l.ReasonTitle ?? "No reason provided",
                        Description = l.Detail ?? "No additional details",
                        Status = l.Status ?? "Pending",
                        StatusColor = (l.Status ?? "Pending") == "Approved" ? "#2FBF4C" :
                                     (l.Status ?? "Pending") == "Rejected" ? "#D23C3C" : "#E3C63A"
                    })
                    .FirstOrDefaultAsync();

                if (leave == null)
                {
                    return NotFound(new { message = "Leave request not found" });
                }

                return Ok(leave);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/LeaveRequests/{id}/approve
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveLeaveRequest(int id)
        {
            try
            {
                var leave = await _context.Leaves.FindAsync(id);
                if (leave == null)
                {
                    return NotFound(new { message = "Leave request not found" });
                }

                leave.Status = "Approved";
                leave.AdminResponse = "Leave request approved";

                await _context.SaveChangesAsync();

                return Ok(new { message = "Leave request approved successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/LeaveRequests/{id}/reject
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> RejectLeaveRequest(int id, [FromBody] RejectRequestDto rejectDto)
        {
            try
            {
                var leave = await _context.Leaves.FindAsync(id);
                if (leave == null)
                {
                    return NotFound(new { message = "Leave request not found" });
                }

                if (string.IsNullOrWhiteSpace(rejectDto.RejectionReason))
                {
                    return BadRequest(new { message = "Rejection reason is required" });
                }

                leave.Status = "Rejected";
                leave.AdminResponse = rejectDto.RejectionReason;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Leave request rejected successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST: api/LeaveRequests
        [HttpPost]
        public async Task<ActionResult<object>> CreateLeaveRequest([FromBody] CreateLeaveRequestDto leaveDto)
        {
            try
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmpCode == leaveDto.EmpCode);

                if (employee == null)
                {
                    return BadRequest(new { message = "Employee not found" });
                }

                var leave = new Leave
                {
                    EmpCode = employee.EmpCode,
                    FromDate = leaveDto.FromDate,
                    ToDate = leaveDto.ToDate,
                    ReasonTitle = leaveDto.ReasonTitle,
                    Detail = leaveDto.Detail,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Leaves.Add(leave);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    leaveId = leave.LeaveID,
                    message = "Leave request created successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DTO classes
        public class RejectRequestDto
        {
            public string RejectionReason { get; set; } = "";
            public int AdminId { get; set; }
        }

        public class CreateLeaveRequestDto
        {
            public string EmpCode { get; set; } = "";
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
            public string ReasonTitle { get; set; } = "";
            public string Detail { get; set; } = "";
        }
    }
}