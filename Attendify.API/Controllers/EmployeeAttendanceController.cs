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
                var startTime = shift.StartTime;
                var endTime = shift.EndTime;

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

            // Default to morning shift if no match found
            return shifts.FirstOrDefault(s => s.Name.Contains("Morning", StringComparison.OrdinalIgnoreCase))
                   ?? shifts.FirstOrDefault()
                   ?? new Shift
                   {
                       Name = "Morning Shift",
                       StartTime = new TimeSpan(8, 0, 0),
                       EndTime = new TimeSpan(12, 30, 0),
                       GracePeriodMinutes = 5,
                       ShiftID = 1
                   };
        }

        private bool CanCheckIn(DateTime currentTime, Shift shift)
        {
            var startTime = shift.StartTime;
            var endTime = shift.EndTime;
            var grace = TimeSpan.FromMinutes(shift.GracePeriodMinutes);

            // Allow check-in from 15 minutes before shift starts until shift ends + grace period
            var checkInStart = startTime.Add(TimeSpan.FromMinutes(-15));
            var checkInEnd = endTime.Add(grace);
            
            var currentTimeOfDay = currentTime.TimeOfDay;

            // Handle overnight shifts
            if (endTime < startTime)
            {
                // Shift spans midnight
                // E.g. 22:00 to 06:00. CheckInStart 21:45. CheckInEnd 06:05 (if grace 5)
                // If current 23:00 -> True (>= 21:45)
                // If current 05:00 -> True (<= 06:00)
                // If current 06:03 -> True (<= 06:05)
                return currentTimeOfDay >= checkInStart || currentTimeOfDay <= checkInEnd;
            }
            else
            {
                // Normal shift
                return currentTimeOfDay >= checkInStart && currentTimeOfDay <= checkInEnd;
            }
        }

        private bool CanCheckOut(DateTime currentTime, Shift shift)
        {
            var endTime = shift.EndTime;

            // Allow check-out 30 minutes before shift ends until 30 minutes after
            var checkOutStart = endTime.Add(TimeSpan.FromMinutes(-30));
            var checkOutEnd = endTime.Add(TimeSpan.FromMinutes(30));
            var currentTimeOfDay = currentTime.TimeOfDay;

            return currentTimeOfDay >= checkOutStart && currentTimeOfDay <= checkOutEnd;
        }

        private bool CheckIfLate(string checkInTimeStr, Shift shift)
        {
            if (string.IsNullOrEmpty(checkInTimeStr) || shift == null) return false;

            if (TimeSpan.TryParse(checkInTimeStr, out var checkIn))
            {
                var shiftEnd = shift.EndTime;
                var graceTime = shiftEnd.Add(TimeSpan.FromMinutes(shift.GracePeriodMinutes));
                
                // If shift spans midnight and checkIn is "early" next day (e.g. 01:00), we need to handle that.
                // But simplified logic as per user request: "5:00 - 5:35 is ontime".
                // This implies strict comparison against the "End Window".
                
                // Note: User logic says "during this 5:00 - 5:30... is ontime".
                // So basic logic: CheckIn > EndTime + Grace => Late.
                
                // However, handling strict timespan comparison for overnight shifts:
                if (shift.EndTime < shift.StartTime)
                {
                     // Overnight
                     // If CheckIn > GraceTime AND CheckIn < StartTime ? Late?
                     // Technically overnight shifts end on Day 2.
                     // A pure TimeSpan compare might fail if we don't account for date.
                     // But assuming standard daily checkin constraint:
                     if (checkIn > graceTime && checkIn < shift.StartTime) return true;
                }
                else
                {
                    if (checkIn > graceTime) return true;
                }
            }

            return false;
        }

        private int CalculateLateMinutes(string checkInTimeStr, Shift shift)
        {
            if (string.IsNullOrEmpty(checkInTimeStr) || shift == null) return 0;

            if (TimeSpan.TryParse(checkInTimeStr, out var checkIn))
            {
                var shiftEnd = shift.EndTime;
                var graceTime = shiftEnd.Add(TimeSpan.FromMinutes(shift.GracePeriodMinutes));
                
                if (shift.EndTime < shift.StartTime)
                {
                     if (checkIn > graceTime && checkIn < shift.StartTime)
                         return (int)(checkIn - graceTime).TotalMinutes;
                }
                else
                {
                    if (checkIn > graceTime)
                    {
                        return (int)(checkIn - graceTime).TotalMinutes;
                    }
                }
            }

            return 0;
        }

        private string FormatTime12Hour(TimeSpan timeSpan)
        {
            var dateTime = DateTime.Today.Add(timeSpan);
            return dateTime.ToString("hh:mm tt", CultureInfo.InvariantCulture);
        }

        private string FormatTime12Hour(string time24Hour)
        {
            if (TimeSpan.TryParse(time24Hour, out var timeSpan))
            {
                return FormatTime12Hour(timeSpan);
            }

            return time24Hour; // Return original if parsing fails
        }

        private ShiftInfo ConvertToShiftInfo(Shift shift)
        {
            return new ShiftInfo
            {
                Name = shift.Name,
                StartTime = shift.StartTime.ToString(@"hh\:mm"),
                EndTime = shift.EndTime.ToString(@"hh\:mm"),
                GracePeriodMinutes = shift.GracePeriodMinutes,
                ShiftID = shift.ShiftID
            };
        }
    }
}