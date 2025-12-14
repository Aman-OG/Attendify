using Attendify.DATA;
using Attendify.DATA.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Attendify.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeAttendanceController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EmployeeAttendanceController> _logger;

        public EmployeeAttendanceController(AppDbContext context, ILogger<EmployeeAttendanceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // DTOs nested inside controller
        public class AttendanceData
        {
            public TodayAttendanceInfo? TodayAttendance { get; set; }
            public List<AttendanceHistoryRecord>? AttendanceHistory { get; set; }
            public MonthlyStats? MonthlyStats { get; set; }
            public ShiftInfo? CurrentShift { get; set; }
            public bool CanCheckIn { get; set; }
            public bool CanCheckOut { get; set; }
        }

        public class TodayAttendanceInfo
        {
            public string TodayDate { get; set; } = null!;
            public string ShiftName { get; set; } = null!;
            public string ShiftTime { get; set; } = null!;
            public int GracePeriodMinutes { get; set; }
            public string CurrentTime { get; set; } = null!;
            public string? CheckInTime { get; set; }
            public string? Status { get; set; }
            public string StatusColor { get; set; } = "#FF6B6B";
            public bool IsCheckedIn { get; set; }
            public bool IsLate { get; set; }
            public int? LateMinutes { get; set; }
        }

        public class AttendanceHistoryRecord
        {
            public string Date { get; set; } = null!;
            public string Shift { get; set; } = null!;
            public string CheckIn { get; set; } = null!;
            public string Status { get; set; } = null!;
            public string StatusColor { get; set; } = "#FF6B6B";
        }

        public class MonthlyStats
        {
            public int DaysPresent { get; set; }
            public int OnTimeRate { get; set; }
            public int LateArrivals { get; set; }
        }

        public class ShiftInfo
        {
            public string Name { get; set; } = null!;
            public string StartTime { get; set; } = null!;
            public string EndTime { get; set; } = null!;
            public int GracePeriodMinutes { get; set; }
            public int ShiftID { get; set; }
        }

        public class CheckInRequest
        {
            public string EmpCode { get; set; } = null!;
            public string CheckInTime { get; set; } = null!;
        }

        public class CheckInResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = null!;
            public string CheckInTime { get; set; } = null!;
            public string Status { get; set; } = null!;
        }

        [HttpGet("data/{empCode}")]
        public async Task<ActionResult<AttendanceData>> GetAttendanceData(string empCode)
        {
            try
            {
                var todayUtc = DateTime.UtcNow.Date;
                var now = DateTime.Now;
                var today = DateTime.Today;

                var result = new AttendanceData();

                // Get current shift based on time
                var currentShift = await GetCurrentShift(now);
                result.CurrentShift = ConvertToShiftInfo(currentShift);

                // Calculate if employee can check in/out
                result.CanCheckIn = CanCheckIn(now, currentShift);
                result.CanCheckOut = CanCheckOut(now, currentShift);

                // Get today's attendance
                var todayAttendance = await _context.Attendance
                    .Include(a => a.AttendancePerShifts)
                        .ThenInclude(aps => aps.Shift)
                    .FirstOrDefaultAsync(a => a.EmpCode == empCode && a.Date.Date == todayUtc);

                var todayInfo = new TodayAttendanceInfo
                {
                    TodayDate = today.ToString("dddd, MMM dd yyyy", CultureInfo.InvariantCulture),
                    ShiftName = currentShift.Name,
                    ShiftTime = $"{FormatTime12Hour(currentShift.StartTime)} – {FormatTime12Hour(currentShift.EndTime)}",
                    GracePeriodMinutes = currentShift.GracePeriodMinutes,
                    CurrentTime = now.ToString("hh:mm:ss tt", CultureInfo.InvariantCulture)
                };

                if (todayAttendance != null)
                {
                    var attendancePerShift = todayAttendance.AttendancePerShifts?.FirstOrDefault();
                    if (attendancePerShift != null && !string.IsNullOrEmpty(attendancePerShift.CheckInTime))
                    {
                        todayInfo.CheckInTime = FormatTime12Hour(attendancePerShift.CheckInTime);
                        todayInfo.Status = attendancePerShift.Status;
                        todayInfo.IsCheckedIn = true;

                        // Check if late
                        bool isLate = CheckIfLate(attendancePerShift.CheckInTime, currentShift);
                        todayInfo.IsLate = isLate;

                        if (isLate)
                        {
                            todayInfo.LateMinutes = CalculateLateMinutes(attendancePerShift.CheckInTime, currentShift);
                            todayInfo.StatusColor = "#FF6B6B";
                        }
                        else
                        {
                            todayInfo.StatusColor = "#4CAF50";
                        }
                    }
                    else
                    {
                        todayInfo.Status = "Not Checked In";
                        todayInfo.IsCheckedIn = false;
                    }
                }
                else
                {
                    todayInfo.Status = "Not Checked In";
                    todayInfo.IsCheckedIn = false;
                }

                result.TodayAttendance = todayInfo;

                // Get attendance history (last 30 days)
                var thirtyDaysAgo = todayUtc.AddDays(-30);
                var attendanceHistory = await _context.Attendance
                    .Include(a => a.AttendancePerShifts)
                        .ThenInclude(aps => aps.Shift)
                    .Where(a => a.EmpCode == empCode && a.Date >= thirtyDaysAgo)
                    .OrderByDescending(a => a.Date)
                    .Take(30)
                    .ToListAsync();

                var historyRecords = new List<AttendanceHistoryRecord>();
                foreach (var att in attendanceHistory)
                {
                    var attendancePerShift = att.AttendancePerShifts?.FirstOrDefault();
                    var shift = attendancePerShift?.Shift;

                    if (attendancePerShift != null && !string.IsNullOrEmpty(attendancePerShift.CheckInTime))
                    {
                        bool isLateForHistory = shift != null && CheckIfLate(attendancePerShift.CheckInTime, shift);

                        historyRecords.Add(new AttendanceHistoryRecord
                        {
                            Date = att.Date.ToString("yyyy-MM-dd"),
                            Shift = shift?.Name ?? "Unknown",
                            CheckIn = FormatTime12Hour(attendancePerShift.CheckInTime),
                            Status = attendancePerShift.Status ?? "Unknown",
                            StatusColor = isLateForHistory ? "#FF6B6B" : "#4CAF50"
                        });
                    }
                }

                result.AttendanceHistory = historyRecords;

                // Calculate monthly stats
                var nowUtc = DateTime.UtcNow;
                var todayUtcDate = nowUtc.Date; // UTC date part only

                var monthStart = new DateTime(todayUtcDate.Year, todayUtcDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var monthlyAttendance = await _context.Attendance
                    .Include(a => a.AttendancePerShifts)
                    .Where(a => a.EmpCode == empCode && a.Date >= monthStart && a.Date <= monthEnd)
                    .ToListAsync();

                int daysPresent = 0;
                int lateArrivals = 0;

                foreach (var att in monthlyAttendance)
                {
                    var attendancePerShift = att.AttendancePerShifts?.FirstOrDefault();
                    if (attendancePerShift != null && !string.IsNullOrEmpty(attendancePerShift.CheckInTime))
                    {
                        daysPresent++;

                        var shift = attendancePerShift.Shift;
                        if (shift != null && CheckIfLate(attendancePerShift.CheckInTime, shift))
                        {
                            lateArrivals++;
                        }
                    }
                }

                int onTimeRate = daysPresent > 0 ? 100 - (lateArrivals * 100 / daysPresent) : 0;

                result.MonthlyStats = new MonthlyStats
                {
                    DaysPresent = daysPresent,
                    LateArrivals = lateArrivals,
                    OnTimeRate = onTimeRate
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance data for employee: {EmpCode}", empCode);
                return StatusCode(500, new { Message = "An error occurred while fetching attendance data" });
            }
        }

        [HttpPost("checkin")]
        public async Task<ActionResult<CheckInResponse>> CheckIn([FromBody] CheckInRequest request)
        {
            try
            {
                var now = DateTime.Now;
                var todayUtc = DateTime.UtcNow.Date;

                // Get current shift
                var currentShift = await GetCurrentShift(now);

                // Check if within check-in time
                if (!CanCheckIn(now, currentShift))
                {
                    return BadRequest(new CheckInResponse
                    {
                        Success = false,
                        Message = "Check-in is not allowed at this time. Please check during your shift hours."
                    });
                }

                // Check if already checked in today
                var existingAttendance = await _context.Attendance
                    .Include(a => a.AttendancePerShifts)
                    .FirstOrDefaultAsync(a => a.EmpCode == request.EmpCode && a.Date.Date == todayUtc);

                if (existingAttendance != null && existingAttendance.AttendancePerShifts?.Any() == true)
                {
                    return BadRequest(new CheckInResponse
                    {
                        Success = false,
                        Message = "You have already checked in today."
                    });
                }

                // Create or update attendance record
                if (existingAttendance == null)
                {
                    existingAttendance = new Attendance
                    {
                        EmpCode = request.EmpCode,
                        Date = todayUtc,
                        Status = "Present",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Attendance.Add(existingAttendance);
                    await _context.SaveChangesAsync();
                }

                // Create attendance per shift record
                var checkInTime12Hour = FormatTime12Hour(request.CheckInTime);
                var checkInTime24Hour = request.CheckInTime; // Assuming frontend sends 24-hour format

                bool isLate = CheckIfLate(checkInTime24Hour, currentShift);
                var status = isLate ? "Late" : "On Time";

                var attendancePerShift = new AttendancePerShift
                {
                    AttendanceID = existingAttendance.AttendanceID,
                    ShiftID = currentShift.ShiftID,
                    CheckInTime = checkInTime24Hour,
                    Status = status,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AttendancePerShift.Add(attendancePerShift);
                await _context.SaveChangesAsync();

                return Ok(new CheckInResponse
                {
                    Success = true,
                    Message = isLate ? "Checked in successfully (Late)" : "Checked in successfully (On Time)",
                    CheckInTime = checkInTime12Hour,
                    Status = status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during check-in for employee: {EmpCode}", request.EmpCode);
                return StatusCode(500, new CheckInResponse
                {
                    Success = false,
                    Message = "An error occurred during check-in"
                });
            }
        }

        private async Task<Shift> GetCurrentShift(DateTime currentTime)
        {
            // Get all shifts
            var shifts = await _context.Shifts.ToListAsync();

            // Find shift that matches current time
            foreach (var shift in shifts)
            {
                if (TimeSpan.TryParse(shift.StartTime, out var startTime) &&
                    TimeSpan.TryParse(shift.EndTime, out var endTime))
                {
                    // Handle overnight shifts
                    if (endTime < startTime)
                    {
                        // Shift spans midnight
                        if (currentTime.TimeOfDay >= startTime || currentTime.TimeOfDay <= endTime)
                        {
                            return shift;
                        }
                    }
                    else
                    {
                        // Normal shift
                        if (currentTime.TimeOfDay >= startTime && currentTime.TimeOfDay <= endTime)
                        {
                            return shift;
                        }
                    }
                }
            }

            // Default to morning shift if no match found
            return shifts.FirstOrDefault(s => s.Name.Contains("Morning", StringComparison.OrdinalIgnoreCase))
                   ?? shifts.FirstOrDefault()
                   ?? new Shift
                   {
                       Name = "Morning Shift",
                       StartTime = "08:00",
                       EndTime = "12:30",
                       GracePeriodMinutes = 5,
                       ShiftID = 1
                   };
        }

        private bool CanCheckIn(DateTime currentTime, Shift shift)
        {
            if (TimeSpan.TryParse(shift.StartTime, out var startTime) &&
                TimeSpan.TryParse(shift.EndTime, out var endTime))
            {
                // Allow check-in from 15 minutes before shift starts until shift ends
                var checkInStart = startTime.Add(TimeSpan.FromMinutes(-15));
                var currentTimeOfDay = currentTime.TimeOfDay;

                // Handle overnight shifts
                if (endTime < startTime)
                {
                    // Shift spans midnight
                    return currentTimeOfDay >= checkInStart || currentTimeOfDay <= endTime;
                }
                else
                {
                    // Normal shift
                    return currentTimeOfDay >= checkInStart && currentTimeOfDay <= endTime;
                }
            }

            return false;
        }

        private bool CanCheckOut(DateTime currentTime, Shift shift)
        {
            if (TimeSpan.TryParse(shift.EndTime, out var endTime))
            {
                // Allow check-out 30 minutes before shift ends until 30 minutes after
                var checkOutStart = endTime.Add(TimeSpan.FromMinutes(-30));
                var checkOutEnd = endTime.Add(TimeSpan.FromMinutes(30));
                var currentTimeOfDay = currentTime.TimeOfDay;

                return currentTimeOfDay >= checkOutStart && currentTimeOfDay <= checkOutEnd;
            }

            return false;
        }

        private bool CheckIfLate(string checkInTime, Shift shift)
        {
            if (string.IsNullOrEmpty(checkInTime) || shift == null) return false;

            if (TimeSpan.TryParse(checkInTime, out var checkIn) &&
                TimeSpan.TryParse(shift.StartTime, out var shiftStart))
            {
                var graceTime = shiftStart.Add(TimeSpan.FromMinutes(shift.GracePeriodMinutes));
                return checkIn > graceTime;
            }

            return false;
        }

        private int CalculateLateMinutes(string checkInTime, Shift shift)
        {
            if (string.IsNullOrEmpty(checkInTime) || shift == null) return 0;

            if (TimeSpan.TryParse(checkInTime, out var checkIn) &&
                TimeSpan.TryParse(shift.StartTime, out var shiftStart))
            {
                var graceTime = shiftStart.Add(TimeSpan.FromMinutes(shift.GracePeriodMinutes));
                if (checkIn > graceTime)
                {
                    return (int)(checkIn - graceTime).TotalMinutes;
                }
            }

            return 0;
        }

        private string FormatTime12Hour(string time24Hour)
        {
            if (TimeSpan.TryParse(time24Hour, out var timeSpan))
            {
                var dateTime = DateTime.Today.Add(timeSpan);
                return dateTime.ToString("hh:mm tt", CultureInfo.InvariantCulture);
            }

            return time24Hour; // Return original if parsing fails
        }

        private ShiftInfo ConvertToShiftInfo(Shift shift)
        {
            return new ShiftInfo
            {
                Name = shift.Name,
                StartTime = shift.StartTime,
                EndTime = shift.EndTime,
                GracePeriodMinutes = shift.GracePeriodMinutes,
                ShiftID = shift.ShiftID
            };
        }
    }
}