using Attendify.DATA;
using Attendify.DATA.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Attendify.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeReportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EmployeeReportsController> _logger;

        public EmployeeReportsController(AppDbContext context, ILogger<EmployeeReportsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // DTOs nested inside controller
        public class EmployeeReportStatsDto
        {
            public double AttendanceRate { get; set; }
            public int DaysPresent { get; set; }
            public int LateArrivals { get; set; }
            public int DaysAbsent { get; set; }
            public int LeavesUsed { get; set; }
        }

        public class MonthlyReportDto
        {
            public string Month { get; set; } = null!;
            public int Present { get; set; }
            public int Late { get; set; }
            public int Absent { get; set; }
            public int LeavesApproved { get; set; }
            public double AttendancePercentage { get; set; }
        }

        public class AttendanceSummaryDto
        {
            public string Date { get; set; } = null!;
            public string Status { get; set; } = null!;
            public string CheckInTime { get; set; } = null!;
            public string Shift { get; set; } = null!;
            public string Color { get; set; } = "#666666";
        }

        public class ApiResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = null!;
            public object? Data { get; set; }
        }

        [HttpGet("stats/{empCode}")]
        public async Task<ActionResult<ApiResponseDto>> GetEmployeeStats(string empCode)
        {
            try
            {
                var currentDate = DateTime.UtcNow;
                var monthStart = new DateTime(currentDate.Year, currentDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                // Get attendance data for current month
                var attendanceData = await _context.Attendance
                    .Include(a => a.AttendancePerShifts)
                    .Where(a => a.EmpCode == empCode && a.Date >= monthStart && a.Date <= monthEnd)
                    .ToListAsync();

                // Get leave data for current month
                var leaveData = await _context.Leaves
                    .Where(l => l.EmpCode == empCode &&
                               l.Status == "Approved" &&
                               l.FromDate >= monthStart &&
                               l.ToDate <= monthEnd)
                    .ToListAsync();

                int totalDays = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
                int daysPresent = 0;
                int lateArrivals = 0;
                int daysAbsent = 0;

                foreach (var attendance in attendanceData)
                {
                    var attendancePerShift = attendance.AttendancePerShifts?.FirstOrDefault();
                    if (attendancePerShift != null && !string.IsNullOrEmpty(attendancePerShift.CheckInTime))
                    {
                        daysPresent++;

                        // Check if late
                        var shift = attendancePerShift.Shift;
                        if (shift != null && CheckIfLate(attendancePerShift.CheckInTime, shift))
                        {
                            lateArrivals++;
                        }
                    }
                    else if (attendance.Status == "Absent" || attendance.Status == "Leave")
                    {
                        daysAbsent++;
                    }
                }

                // Calculate leaves used (sum of days for approved leaves in current month)
                int leavesUsed = 0;
                foreach (var leave in leaveData)
                {
                    if (leave.FromDate.HasValue && leave.ToDate.HasValue)
                    {
                        var fromDate = leave.FromDate.Value;
                        var toDate = leave.ToDate.Value;

                        // Count only days within current month
                        var startDate = fromDate < monthStart ? monthStart : fromDate;
                        var endDate = toDate > monthEnd ? monthEnd : toDate;

                        if (startDate <= endDate)
                        {
                            leavesUsed += (int)(endDate - startDate).TotalDays + 1;
                        }
                    }
                }

                double attendanceRate = totalDays > 0 ? (double)daysPresent / totalDays * 100 : 0;

                var stats = new EmployeeReportStatsDto
                {
                    AttendanceRate = Math.Round(attendanceRate, 1),
                    DaysPresent = daysPresent,
                    LateArrivals = lateArrivals,
                    DaysAbsent = daysAbsent,
                    LeavesUsed = leavesUsed
                };

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Employee stats retrieved successfully",
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee stats: {EmpCode}", empCode);
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "An error occurred while fetching employee statistics"
                });
            }
        }

        [HttpGet("monthly-report/{empCode}")]
        public async Task<ActionResult<ApiResponseDto>> GetMonthlyReport(string empCode)
        {
            try
            {
                var currentDate = DateTime.UtcNow;
                var monthlyReports = new List<MonthlyReportDto>();

                // Get data for last 6 months
                for (int i = 5; i >= 0; i--)
                {
                    var reportDate = currentDate.AddMonths(-i);
                    var monthStart = new DateTime(reportDate.Year, reportDate.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    // Get attendance for the month
                    var attendanceData = await _context.Attendance
                        .Include(a => a.AttendancePerShifts)
                        .Where(a => a.EmpCode == empCode && a.Date >= monthStart && a.Date <= monthEnd)
                        .ToListAsync();

                    // Get approved leaves for the month
                    var leaveData = await _context.Leaves
                        .Where(l => l.EmpCode == empCode &&
                                   l.Status == "Approved" &&
                                   l.FromDate >= monthStart &&
                                   l.ToDate <= monthEnd)
                        .ToListAsync();

                    int totalDays = DateTime.DaysInMonth(reportDate.Year, reportDate.Month);
                    int present = 0;
                    int late = 0;
                    int absent = 0;
                    int leavesApproved = 0;

                    foreach (var attendance in attendanceData)
                    {
                        var attendancePerShift = attendance.AttendancePerShifts?.FirstOrDefault();
                        if (attendancePerShift != null && !string.IsNullOrEmpty(attendancePerShift.CheckInTime))
                        {
                            present++;

                            // Check if late
                            var shift = attendancePerShift.Shift;
                            if (shift != null && CheckIfLate(attendancePerShift.CheckInTime, shift))
                            {
                                late++;
                            }
                        }
                        else if (attendance.Status == "Absent")
                        {
                            absent++;
                        }
                    }

                    // Count leaves approved
                    foreach (var leave in leaveData)
                    {
                        if (leave.FromDate.HasValue && leave.ToDate.HasValue)
                        {
                            leavesApproved += (int)(leave.ToDate.Value - leave.FromDate.Value).TotalDays + 1;
                        }
                    }

                    double attendancePercentage = totalDays > 0 ? (double)present / totalDays * 100 : 0;

                    monthlyReports.Add(new MonthlyReportDto
                    {
                        Month = reportDate.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                        Present = present,
                        Late = late,
                        Absent = absent,
                        LeavesApproved = leavesApproved,
                        AttendancePercentage = Math.Round(attendancePercentage, 1)
                    });
                }

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Monthly report retrieved successfully",
                    Data = monthlyReports
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly report: {EmpCode}", empCode);
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "An error occurred while fetching monthly report"
                });
            }
        }

        [HttpGet("attendance-summary/{empCode}")]
        public async Task<ActionResult<ApiResponseDto>> GetAttendanceSummary(string empCode)
        {
            try
            {
                var currentDate = DateTime.UtcNow;
                var thirtyDaysAgo = currentDate.AddDays(-30);

                var attendanceData = await _context.Attendance
                    .Include(a => a.AttendancePerShifts)
                        .ThenInclude(aps => aps.Shift)
                    .Where(a => a.EmpCode == empCode && a.Date >= thirtyDaysAgo)
                    .OrderByDescending(a => a.Date)
                    .Take(30)
                    .ToListAsync();

                var summary = new List<AttendanceSummaryDto>();

                foreach (var attendance in attendanceData)
                {
                    var attendancePerShift = attendance.AttendancePerShifts?.FirstOrDefault();
                    var shift = attendancePerShift?.Shift;

                    string status = "Absent";
                    string checkInTime = "N/A";
                    string color = "#666666";
                    string shiftName = "N/A";

                    if (attendancePerShift != null && !string.IsNullOrEmpty(attendancePerShift.CheckInTime))
                    {
                        checkInTime = FormatTime12Hour(attendancePerShift.CheckInTime);
                        shiftName = shift?.Name ?? "N/A";

                        if (shift != null && CheckIfLate(attendancePerShift.CheckInTime, shift))
                        {
                            status = "Late";
                            color = "#E3C63A";
                        }
                        else
                        {
                            status = "Present";
                            color = "#2FBF4C";
                        }
                    }
                    else if (attendance.Status == "Leave")
                    {
                        status = "Leave";
                        color = "#A95315";
                    }

                    summary.Add(new AttendanceSummaryDto
                    {
                        Date = attendance.Date.ToString("MMM dd, yyyy"),
                        Status = status,
                        CheckInTime = checkInTime,
                        Shift = shiftName,
                        Color = color
                    });
                }

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Attendance summary retrieved successfully",
                    Data = summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance summary: {EmpCode}", empCode);
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "An error occurred while fetching attendance summary"
                });
            }
        }

        private bool CheckIfLate(string checkInTimeStr, Shift shift)
        {
            if (string.IsNullOrEmpty(checkInTimeStr) || shift == null) return false;

            if (TimeSpan.TryParse(checkInTimeStr, out var checkIn))
            {
                var shiftStart = shift.StartTime;
                var graceTime = shiftStart.Add(TimeSpan.FromMinutes(shift.GracePeriodMinutes));
                return checkIn > graceTime;
            }

            return false;
        }

        private string FormatTime12Hour(string time24Hour)
        {
            if (TimeSpan.TryParse(time24Hour, out var timeSpan))
            {
                var dateTime = DateTime.Today.Add(timeSpan);
                return dateTime.ToString("hh:mm tt", CultureInfo.InvariantCulture);
            }

            return time24Hour;
        }
    }
}