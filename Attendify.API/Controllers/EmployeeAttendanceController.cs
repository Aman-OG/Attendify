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

            // Allow check-in from 1 hour before shift starts until shift ends
            // User requirement: "button should be active before one hour of start time"
            var checkInStart = startTime.Add(TimeSpan.FromHours(-1));
            // User requirement: "late = from the end of grace minutes to end of time shifts"
            // Implications: One can check in until the shift ends (just marked as late).
            var checkInEnd = endTime;
            
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
                // Logic: On Time = [CheckInStart, StartTime + Grace]
                // Late = (StartTime + Grace, EndTime]
                
                var shiftStart = shift.StartTime;
                var graceTime = shiftStart.Add(TimeSpan.FromMinutes(shift.GracePeriodMinutes));

                if (shift.EndTime < shift.StartTime)
                {
                     // Overnight shift
                     // Example: Start 22:00, End 06:00. Grace 10m -> 22:10.
                     // On Time: 21:00 ... 22:10.
                     // Late: 22:10 ... 06:00
                     
                     // If CheckIn is small (e.g. 05:00), it's definitely "late" relative to 22:00 start (conceptually next day)
                     // If CheckIn is large (e.g. 23:00), it's > 22:10, so Late.
                     
                     // If checkIn < start (is 00:00 - 06:00) -> Late
                     if (checkIn < shiftStart && checkIn <= shift.EndTime) return true;
                     
                     // If checkIn > grace -> Late
                     if (checkIn > graceTime) return true;
                }
                else
                {
                    // Normal shift
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
                var shiftStart = shift.StartTime;
                var graceTime = shiftStart.Add(TimeSpan.FromMinutes(shift.GracePeriodMinutes));
                
                // Logic: Late Minutes = CheckIn - (Start + Grace) ?? Or just CheckIn - Start?
                // Usually "Late Minutes" means how much AFTER the start time.
                // But if they are within grace, late = 0.
                // If they are past grace, late = Total minutes past Start time? Or past grace?
                // Standard HR: late is from StartTime. 
                // However, user said "on time = ... + grace minutes".
                // Let's count minutes past Start Time for simplicity, but only if IsLate is true.
                
                // Simplified per user request implied logic:
                // late = from end of grace minutes... 
                // So let's calculate diff from GraceTime boundary or StartTime?
                // Usually it makes sense to calc from StartTime.
                
                if (CheckIfLate(checkInTimeStr, shift))
                {
                     // Handle overnight
                    if (shift.EndTime < shift.StartTime)
                    {
                        if (checkIn < shiftStart) // Next day part (01:00)
                        {
                            // Time from Start(22:00) to Midnight(24:00) + CheckIn(01:00)
                            double minutes = (24 * 60) - shiftStart.TotalMinutes + checkIn.TotalMinutes;
                            return (int)minutes;
                        }
                        else
                        {
                            return (int)(checkIn - shiftStart).TotalMinutes;
                        }
                    }
                    else
                    {
                        return (int)(checkIn - shiftStart).TotalMinutes;
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