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
    public class AttendanceController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AttendanceController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/attendance
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAttendance(
            [FromQuery] DateTime? date = null,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] string? department = null)
        {
            try
            {
                Console.WriteLine($"GetAttendance called with date: {date}, status: {status}, search: {search}, department: {department}");

                // FIXED: Ensure the date has UTC kind
                DateTime queryDate;
                if (date.HasValue)
                {
                    if (date.Value.Kind == DateTimeKind.Utc)
                    {
                        queryDate = date.Value.Date;
                    }
                    else if (date.Value.Kind == DateTimeKind.Local)
                    {
                        queryDate = date.Value.ToUniversalTime().Date;
                    }
                    else
                    {
                        queryDate = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc);
                    }
                }
                else
                {
                    queryDate = DateTime.UtcNow.Date;
                }

                Console.WriteLine($"Query date (UTC): {queryDate}");

                // Get all employees
                Console.WriteLine("Getting employees...");
                var employees = await _context.Employees
                    .Where(e => e.IsActive)
                    .ToListAsync();
                Console.WriteLine($"Found {employees.Count} active employees");

                if (!employees.Any())
                {
                    Console.WriteLine("No active employees found!");
                    return Ok(new List<object>()); // Return empty list instead of error
                }

                // Get attendance for the date
                Console.WriteLine("Getting attendance records...");
                var attendanceRecords = await _context.Attendance
                    .Include(a => a.Employee)
                    .Where(a => a.Date.Date == queryDate.Date)
                    .ToListAsync();
                Console.WriteLine($"Found {attendanceRecords.Count} attendance records for {queryDate}");

                // Create result with all employees and their attendance status
                var result = new List<object>();
                Console.WriteLine("Processing employees...");

                foreach (var employee in employees)
                {
                    try
                    {
                        if (employee == null)
                        {
                            Console.WriteLine("Warning: Null employee encountered");
                            continue;
                        }

                        var attendance = attendanceRecords.FirstOrDefault(a => a.EmpCode == employee.EmpCode);

                        string statusText = "Absent"; // Default status
                        string statusColor = "#DC3545"; // Red for absent

                        if (attendance != null)
                        {
                            statusText = attendance.Status ?? "Absent";
                            statusColor = attendance.Status == "Present" ? "#28A745" :
                                         attendance.Status == "Late" ? "#FFC107" :
                                         attendance.Status == "On Leave" ? "#007BFF" :
                                         attendance.Status == "Half Day" ? "#6F42C1" : "#DC3545";
                        }

                        result.Add(new
                        {
                            AttendanceID = attendance?.AttendanceID ?? 0,
                            EmployeeID = employee.EmpCode ?? "N/A",
                            FirstName = employee.FirstName ?? "Unknown",
                            MiddleName = employee.MiddleName ?? "",
                            Department = employee.Department ?? "N/A",
                            Position = employee.Position ?? "N/A",
                            Date = queryDate.ToString("yyyy-MM-dd"), // Return as string
                            Status = statusText,
                            StatusColor = statusColor,
                            CheckInTime = attendance?.CreatedAt.ToString("HH:mm") ?? "N/A"
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing employee {employee?.EmpCode}: {ex.Message}");
                        // Continue with next employee
                    }
                }

                Console.WriteLine($"Processed {result.Count} records");

                // Apply filters
                if (!string.IsNullOrEmpty(status) && status != "All")
                {
                    result = result.Where(r => ((dynamic)r).Status == status).ToList();
                    Console.WriteLine($"Applied status filter '{status}': {result.Count} records remain");
                }

                if (!string.IsNullOrEmpty(department) && department != "All")
                {
                    result = result.Where(r => ((dynamic)r).Department == department).ToList();
                    Console.WriteLine($"Applied department filter '{department}': {result.Count} records remain");
                }

                if (!string.IsNullOrEmpty(search))
                {
                    var term = search.ToLower();
                    result = result.Where(r =>
                        ((dynamic)r).EmployeeID.ToLower().Contains(term) ||
                        ((dynamic)r).FirstName.ToLower().Contains(term) ||
                        ((dynamic)r).LastName.ToLower().Contains(term) ||
                        ((dynamic)r).Department.ToLower().Contains(term)).ToList();
                    Console.WriteLine($"Applied search filter '{search}': {result.Count} records remain");
                }

                Console.WriteLine($"Returning {result.Count} records");
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRITICAL ERROR in GetAttendance: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/attendance/stats
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetAttendanceStats([FromQuery] DateTime? date = null)
        {
            try
            {
                // FIXED: Ensure the date has UTC kind
                DateTime utcDate;
                if (date.HasValue)
                {
                    if (date.Value.Kind == DateTimeKind.Utc)
                    {
                        utcDate = date.Value.Date;
                    }
                    else if (date.Value.Kind == DateTimeKind.Local)
                    {
                        utcDate = date.Value.ToUniversalTime().Date;
                    }
                    else
                    {
                        utcDate = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc);
                    }
                }
                else
                {
                    utcDate = DateTime.UtcNow.Date;
                }

                var totalEmployees = await _context.Employees.CountAsync(e => e.IsActive);

                var attendanceRecords = await _context.Attendance
                    .Where(a => a.Date.Date == utcDate.Date)
                    .ToListAsync();

                var presentCount = attendanceRecords.Count(a => a.Status == "Present");
                var lateCount = attendanceRecords.Count(a => a.Status == "Late");
                var onLeaveCount = attendanceRecords.Count(a => a.Status == "On Leave");
                var halfDayCount = attendanceRecords.Count(a => a.Status == "Half Day");
                var absentCount = totalEmployees - (presentCount + lateCount + onLeaveCount + halfDayCount);

                // FIXED: Return date as string to avoid DateTime serialization issues
                return Ok(new
                {
                    Date = utcDate.ToString("yyyy-MM-dd"), // Return as string, not DateTime
                    TotalEmployees = totalEmployees,
                    Present = presentCount,
                    Late = lateCount,
                    OnLeave = onLeaveCount,
                    HalfDay = halfDayCount,
                    Absent = absentCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/attendance/employee/{empCode}
        [HttpGet("employee/{empCode}")]
        public async Task<ActionResult<object>> GetEmployeeAttendance(string empCode, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // FIXED: Ensure dates have UTC kind
                DateTime start, end;

                if (startDate.HasValue)
                {
                    if (startDate.Value.Kind == DateTimeKind.Utc)
                    {
                        start = startDate.Value;
                    }
                    else if (startDate.Value.Kind == DateTimeKind.Local)
                    {
                        start = startDate.Value.ToUniversalTime();
                    }
                    else
                    {
                        start = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
                    }
                }
                else
                {
                    start = DateTime.UtcNow.AddDays(-30);
                }

                if (endDate.HasValue)
                {
                    if (endDate.Value.Kind == DateTimeKind.Utc)
                    {
                        end = endDate.Value;
                    }
                    else if (endDate.Value.Kind == DateTimeKind.Local)
                    {
                        end = endDate.Value.ToUniversalTime();
                    }
                    else
                    {
                        end = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
                    }
                }
                else
                {
                    end = DateTime.UtcNow;
                }

                var attendance = await _context.Attendance
                    .Include(a => a.Employee)
                    .Where(a => a.EmpCode == empCode && a.Date >= start && a.Date <= end)
                    .OrderByDescending(a => a.Date)
                    .Select(a => new
                    {
                        a.AttendanceID,
                        a.EmpCode,
                        EmployeeName = a.Employee.FirstName + " " + a.Employee.LastName,
                        Date = a.Date.ToString("yyyy-MM-dd"), // Return as string
                        a.Status,
                        // Use conditional operator instead of switch
                        StatusColor = a.Status == "Present" ? "#28A745" :
                                    a.Status == "Late" ? "#FFC107" :
                                    a.Status == "On Leave" ? "#007BFF" :
                                    a.Status == "Half Day" ? "#6F42C1" : "#DC3545",
                        CheckInTime = a.CreatedAt.ToString("HH:mm")
                    })
                    .ToListAsync();

                return Ok(attendance);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST: api/attendance/checkin
        [HttpPost("checkin")]
        public async Task<ActionResult<object>> CheckIn([FromBody] CheckInDto checkInDto)
        {
            try
            {
                var currentUtcDate = DateTime.UtcNow.Date;

                // Check if already checked in today
                var existing = await _context.Attendance
                    .FirstOrDefaultAsync(a => a.EmpCode == checkInDto.EmpCode &&
                                             a.Date.Date == currentUtcDate);

                if (existing != null)
                {
                    return BadRequest(new { message = "Already checked in today" });
                }

                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmpCode == checkInDto.EmpCode);

                if (employee == null)
                {
                    return NotFound(new { message = "Employee not found" });
                }

                var attendance = new Attendance
                {
                    EmpCode = checkInDto.EmpCode,
                    Date = currentUtcDate,
                    Status = checkInDto.Status,
                    CreatedAt = DateTime.UtcNow
                };

                // FIXED: Ensure DateTimeKind is UTC
                if (attendance.CreatedAt.Kind == DateTimeKind.Unspecified)
                {
                    attendance.CreatedAt = DateTime.SpecifyKind(attendance.CreatedAt, DateTimeKind.Utc);
                }

                _context.Attendance.Add(attendance);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Checked in successfully",
                    attendanceId = attendance.AttendanceID,
                    employeeName = employee.FirstName + " " + employee.LastName,
                    date = attendance.Date.ToString("yyyy-MM-dd"), // Return as string
                    status = attendance.Status,
                    checkInTime = attendance.CreatedAt.ToString("HH:mm")
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/attendance/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAttendance(int id, [FromBody] UpdateAttendanceDto updateDto)
        {
            try
            {
                var attendance = await _context.Attendance.FindAsync(id);
                if (attendance == null)
                {
                    return NotFound(new { message = "Attendance record not found" });
                }

                attendance.Status = updateDto.Status;

                if (updateDto.CheckOutTime.HasValue)
                {
                    // Ensure UTC kind if storing DateTime
                    var checkOutTime = updateDto.CheckOutTime.Value;
                    if (checkOutTime.Kind == DateTimeKind.Unspecified)
                    {
                        checkOutTime = DateTime.SpecifyKind(checkOutTime, DateTimeKind.Utc);
                    }
                    else if (checkOutTime.Kind == DateTimeKind.Local)
                    {
                        checkOutTime = checkOutTime.ToUniversalTime();
                    }

                    // You might want to store checkout time in a separate field
                    // For now, we'll just update the status
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Attendance updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DTO Classes
        public class CheckInDto
        {
            public string EmpCode { get; set; } = "";
            public string Status { get; set; } = "Present"; // Present, Late, etc.
            public string? Notes { get; set; }
        }

        public class UpdateAttendanceDto
        {
            public string Status { get; set; } = "";
            public DateTime? CheckOutTime { get; set; }
            public string? Notes { get; set; }
        }
    }
}