using Attendify.DATA;
using Attendify.DATA.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Attendify.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeHomeController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EmployeeHomeController> _logger;

        public EmployeeHomeController(AppDbContext context, ILogger<EmployeeHomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // DTOs nested inside controller
        public class EmployeeHomeData
        {
            public EmployeeInfo? EmployeeInfo { get; set; }
            public TodayAttendance? TodayAttendance { get; set; }
            public NextShiftInfo? NextShift { get; set; }
            public LeaveStatusInfo? LeaveStatus { get; set; }
            public List<AdminMessageInfo>? AdminMessages { get; set; }
            public List<RecentActivityInfo>? RecentActivities { get; set; }
        }

        public class EmployeeInfo
        {
            public string FirstName { get; set; } = null!;
            public string? MiddleName { get; set; }
            public string LastName { get; set; } = null!;
            public string? Department { get; set; }
            public string? Position { get; set; }
            public string? Email { get; set; }
        }

        public class TodayAttendance
        {
            public string? CheckInTime { get; set; }
            public string? Status { get; set; }
            public string? ShiftName { get; set; }
            public int? GracePeriodMinutes { get; set; }
            public bool IsLate { get; set; }
            public int? LateMinutes { get; set; }
            public bool HasCheckedIn { get; set; }
        }

        public class NextShiftInfo
        {
            public string? ShiftName { get; set; }
            public string? StartTime { get; set; }
            public string? EndTime { get; set; }
            public string? TimeUntilStart { get; set; }
        }

        public class LeaveStatusInfo
        {
            public int PendingCount { get; set; }
            public int ApprovedCount { get; set; }
            public int RejectedCount { get; set; }
            public int RemainingDays { get; set; }
            public string RemainingText { get; set; } = string.Empty;
        }

        public class AdminMessageInfo
        {
            public string Title { get; set; } = null!;
            public string Body { get; set; } = null!;
            public string Type { get; set; } = null!;
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class RecentActivityInfo
        {
            public string ActivityType { get; set; } = null!;
            public string Description { get; set; } = null!;
            public string TimeAgo { get; set; } = null!;
            public DateTime CreatedAt { get; set; }
        }

        [HttpGet("dashboard/{empCode}")]
        public async Task<ActionResult<EmployeeHomeData>> GetDashboardData(string empCode)
        {
            try
            {
                // Use UTC for database queries
                var todayUtc = DateTime.UtcNow.Date;
                var nowUtc = DateTime.UtcNow;

                // Get employee info
                var employee = await _context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmpCode == empCode && e.IsActive);

                if (employee == null)
                {
                    return NotFound(new { Message = "Employee not found" });
                }

                var result = new EmployeeHomeData
                {
                    EmployeeInfo = new EmployeeInfo
                    {
                        FirstName = employee.FirstName,
                        MiddleName = employee.MiddleName,
                        LastName = employee.LastName,
                        Department = employee.Department,
                        Position = employee.Position,
                        Email = employee.Email
                    }
                };

                // Get today's attendance with AttendancePerShift and Shift
                var todayAttendance = await _context.Attendance
                    .AsNoTracking()
                    .Include(a => a.AttendancePerShifts)
                        .ThenInclude(aps => aps.Shift)
                    .FirstOrDefaultAsync(a => a.EmpCode == empCode &&
                                            a.Date.Date == todayUtc);

                if (todayAttendance != null)
                {
                    var attendancePerShift = todayAttendance.AttendancePerShifts?.FirstOrDefault();
                    var shift = attendancePerShift?.Shift;

                    // Parse check-in time from string
                    TimeSpan? checkInTime = null;
                    string formattedCheckInTime = "Not checked in";
                    bool hasCheckedIn = false;

                    if (!string.IsNullOrEmpty(attendancePerShift?.CheckInTime))
                    {
                        if (TimeSpan.TryParse(attendancePerShift.CheckInTime, out var parsedTime))
                        {
                            checkInTime = parsedTime;
                            // Format check-in time for display (e.g., "08:15 AM")
                            var checkInDateTime = DateTime.Today.Add(parsedTime);
                            formattedCheckInTime = checkInDateTime.ToString("hh:mm tt", CultureInfo.InvariantCulture);
                            hasCheckedIn = true;
                        }
                    }

                    // Calculate if late based on shift start time
                    bool isLate = false;
                    int? lateMinutes = null;

                    if (checkInTime.HasValue && shift != null &&
                        !string.IsNullOrEmpty(shift.StartTime))
                    {
                        if (TimeSpan.TryParse(shift.StartTime, out var shiftStartTime))
                        {
                            var gracePeriod = TimeSpan.FromMinutes(shift.GracePeriodMinutes);
                            var actualStartTime = shiftStartTime.Add(gracePeriod);

                            if (checkInTime > actualStartTime)
                            {
                                isLate = true;
                                lateMinutes = (int)(checkInTime.Value - actualStartTime).TotalMinutes;
                            }
                        }
                    }

                    result.TodayAttendance = new TodayAttendance
                    {
                        Status = attendancePerShift?.Status ?? todayAttendance.Status,
                        CheckInTime = hasCheckedIn ? formattedCheckInTime : "Not checked in",
                        HasCheckedIn = hasCheckedIn,
                        ShiftName = shift?.Name,
                        GracePeriodMinutes = shift?.GracePeriodMinutes,
                        IsLate = isLate,
                        LateMinutes = lateMinutes
                    };
                }
                else
                {
                    // No attendance for today
                    result.TodayAttendance = new TodayAttendance
                    {
                        Status = "Not checked in",
                        CheckInTime = "Not checked in",
                        HasCheckedIn = false,
                        GracePeriodMinutes = 5 // Default
                    };
                }

                // Get ACTUAL next shift from database (not hardcoded)
                var shifts = await _context.Shifts
                    .AsNoTracking()
                    .ToListAsync();

                Shift? nextShift = null;
                if (shifts.Any())
                {
                    // Simple logic: Get the first shift (or implement your own logic)
                    // For now, get "Morning Shift" or the first available
                    nextShift = shifts.FirstOrDefault(s => s.Name.Contains("Morning", StringComparison.OrdinalIgnoreCase))
                              ?? shifts.FirstOrDefault();
                }

                if (nextShift != null)
                {
                    // Use the ACTUAL times from database
                    string startTime = nextShift.StartTime ?? "08:00";
                    string endTime = nextShift.EndTime ?? "12:30"; // Default to your actual end time

                    // Calculate time until start
                    string timeUntil = CalculateTimeUntilNextShift(startTime);

                    result.NextShift = new NextShiftInfo
                    {
                        ShiftName = nextShift.Name,
                        StartTime = startTime,
                        EndTime = endTime,
                        TimeUntilStart = timeUntil
                    };
                }
                else
                {
                    // Default values that match your database
                    result.NextShift = new NextShiftInfo
                    {
                        ShiftName = "Morning Shift",
                        StartTime = "08:00",
                        EndTime = "12:30", // Your actual end time
                        TimeUntilStart = CalculateTimeUntilNextShift("08:00")
                    };
                }

                // Get leave status
                var leaves = await _context.Leaves
                    .AsNoTracking()
                    .Where(l => l.EmpCode == empCode)
                    .ToListAsync();

                var leaveInfo = CalculateLeaveStatus(leaves);
                result.LeaveStatus = new LeaveStatusInfo
                {
                    PendingCount = leaveInfo.PendingCount,
                    ApprovedCount = leaveInfo.ApprovedCount,
                    RejectedCount = leaveInfo.RejectedCount,
                    RemainingDays = leaveInfo.RemainingDays,
                    RemainingText = leaveInfo.RemainingText
                };

                // Get recent admin messages
                var messages = await _context.AdminMessages
                    .AsNoTracking()
                    .Where(m => m.IsActive)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                result.AdminMessages = messages.Select(m => new AdminMessageInfo
                {
                    Title = m.Title,
                    Body = m.Body,
                    Type = m.Type,
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt
                }).ToList();

                // Get recent activities
                result.RecentActivities = await GetRecentActivities(empCode);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data for employee: {EmpCode}", empCode);
                return StatusCode(500, new { Message = "An error occurred while fetching dashboard data" });
            }
        }

        private string CalculateTimeUntilNextShift(string startTime)
        {
            if (TimeSpan.TryParse(startTime, out var shiftStartTime))
            {
                var localNow = DateTime.Now;
                var startTimeToday = new DateTime(localNow.Year, localNow.Month, localNow.Day)
                    .Add(shiftStartTime);

                if (startTimeToday > localNow)
                {
                    var timeUntil = startTimeToday - localNow;

                    if (timeUntil.TotalHours >= 1)
                        return $"Starts in {Math.Ceiling(timeUntil.TotalHours)} hours";
                    else if (timeUntil.TotalMinutes >= 1)
                        return $"Starts in {Math.Ceiling(timeUntil.TotalMinutes)} minutes";
                    else
                        return "Starting soon";
                }
                else
                {
                    // Shift has already started
                    var timeSinceStart = localNow - startTimeToday;
                    if (timeSinceStart.TotalHours < 1)
                        return $"Started {Math.Floor(timeSinceStart.TotalMinutes)} minutes ago";
                    else
                        return "In progress";
                }
            }

            return "Schedule not available";
        }

        private (int PendingCount, int ApprovedCount, int RejectedCount, int RemainingDays, string RemainingText)
            CalculateLeaveStatus(List<Leave> leaves)
        {
            int pendingCount = leaves.Count(l => l.Status == "Pending");
            int approvedCount = leaves.Count(l => l.Status == "Approved");
            int rejectedCount = leaves.Count(l => l.Status == "Rejected");

            // Calculate used leave days
            int usedDays = 0;
            foreach (var leave in leaves.Where(l => l.Status == "Approved"))
            {
                if (leave.FromDate.HasValue && leave.ToDate.HasValue)
                {
                    usedDays += (int)(leave.ToDate.Value - leave.FromDate.Value).TotalDays + 1;
                }
            }

            // Calculate remaining days for THIS MONTH
            int remainingDays = CalculateRemainingLeaveDaysThisMonth(leaves);
            string remainingText = $"{remainingDays} days this month";

            return (pendingCount, approvedCount, rejectedCount, remainingDays, remainingText);
        }

        private int CalculateRemainingLeaveDaysThisMonth(List<Leave> leaves)
        {
            var now = DateTime.Now;
            var currentMonth = now.Month;
            var currentYear = now.Year;
            var daysInMonth = DateTime.DaysInMonth(currentYear, currentMonth);
            var today = now.Day;

            // Assuming standard workdays (Monday-Friday)
            int totalWorkdaysThisMonth = 0;
            int remainingWorkdays = 0;

            // Calculate workdays for the whole month and remaining
            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(currentYear, currentMonth, day);
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    totalWorkdaysThisMonth++;
                    if (day >= today)
                    {
                        remainingWorkdays++;
                    }
                }
            }

            // Calculate approved leave days for this month
            int usedLeaveDaysThisMonth = 0;
            foreach (var leave in leaves.Where(l => l.Status == "Approved"))
            {
                if (leave.FromDate.HasValue && leave.ToDate.HasValue)
                {
                    var fromDate = leave.FromDate.Value;
                    var toDate = leave.ToDate.Value;

                    // Check if leave overlaps with current month
                    if ((fromDate.Year == currentYear && fromDate.Month == currentMonth) ||
                        (toDate.Year == currentYear && toDate.Month == currentMonth))
                    {
                        // Calculate overlap with current month
                        var start = fromDate < new DateTime(currentYear, currentMonth, 1)
                            ? new DateTime(currentYear, currentMonth, 1)
                            : fromDate;
                        var end = toDate > new DateTime(currentYear, currentMonth, daysInMonth)
                            ? new DateTime(currentYear, currentMonth, daysInMonth)
                            : toDate;

                        if (start <= end)
                        {
                            usedLeaveDaysThisMonth += (int)(end - start).TotalDays + 1;
                        }
                    }
                }
            }

            // Calculate remaining leave days (workdays - used leave days)
            int remainingLeaveDays = Math.Max(0, totalWorkdaysThisMonth - usedLeaveDaysThisMonth);

            return remainingLeaveDays;
        }

        private async Task<List<RecentActivityInfo>> GetRecentActivities(string empCode)
        {
            var activities = new List<RecentActivityInfo>();

            try
            {
                // Get recent attendance with check-in times
                var recentAttendances = await _context.Attendance
                    .AsNoTracking()
                    .Include(a => a.AttendancePerShifts)
                    .Where(a => a.EmpCode == empCode)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(3)
                    .ToListAsync();

                foreach (var att in recentAttendances)
                {
                    var checkInTime = att.AttendancePerShifts?.FirstOrDefault()?.CheckInTime;
                    string timeText = "Not checked in";
                    if (!string.IsNullOrEmpty(checkInTime))
                    {
                        if (TimeSpan.TryParse(checkInTime, out var parsedTime))
                        {
                            var checkInDateTime = DateTime.Today.Add(parsedTime);
                            timeText = checkInDateTime.ToString("hh:mm tt", CultureInfo.InvariantCulture);
                        }
                    }

                    activities.Add(new RecentActivityInfo
                    {
                        ActivityType = "Attendance",
                        Description = $"Clocked in on {att.Date:MMM dd} at {timeText}",
                        TimeAgo = GetTimeAgo(att.CreatedAt),
                        CreatedAt = att.CreatedAt
                    });
                }

                // Get recent leaves
                var recentLeaves = await _context.Leaves
                    .AsNoTracking()
                    .Where(l => l.EmpCode == empCode)
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(3)
                    .ToListAsync();

                foreach (var leave in recentLeaves)
                {
                    var dateRange = leave.FromDate.HasValue && leave.ToDate.HasValue
                        ? $"{leave.FromDate:MMM dd} - {leave.ToDate:MMM dd}"
                        : "Date not set";

                    var statusText = !string.IsNullOrEmpty(leave.Status)
                        ? $" ({leave.Status})"
                        : "";

                    activities.Add(new RecentActivityInfo
                    {
                        ActivityType = "Leave",
                        Description = $"Leave request for {dateRange}{statusText}",
                        TimeAgo = GetTimeAgo(leave.CreatedAt),
                        CreatedAt = leave.CreatedAt
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activities for employee: {EmpCode}", empCode);
            }

            return activities.OrderByDescending(a => a.CreatedAt).Take(5).ToList();
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays} day{(timeSpan.TotalDays >= 2 ? "s" : "")} ago";

            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours} hour{(timeSpan.TotalHours >= 2 ? "s" : "")} ago";

            if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes} minute{(timeSpan.TotalMinutes >= 2 ? "s" : "")} ago";

            return "Just now";
        }
    }
}