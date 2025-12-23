using Attendify.DATA;
using Attendify.DATA.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendify.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeShiftsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EmployeeShiftsController> _logger;

        public EmployeeShiftsController(AppDbContext context, ILogger<EmployeeShiftsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // DTOs
        public class ShiftResponseDto
        {
            public int ShiftId { get; set; }
            public string Name { get; set; } = null!;
            public string StartTime { get; set; } = null!;
            public string EndTime { get; set; } = null!;
            public int GracePeriodMinutes { get; set; }
            public bool IsCurrentlyActive { get; set; }
            public string DisplayTime { get; set; } = null!;
            public string StatusColor { get; set; } = "#38b000";
        }

        public class ApiResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = null!;
            public object? Data { get; set; }
        }

        [HttpGet("shifts")]
        public async Task<ActionResult<ApiResponseDto>> GetShifts()
        {
            try
            {
                var shifts = await _context.Shifts
                    .OrderBy(s => s.StartTime)
                    .ToListAsync();

                var currentTime = DateTime.Now.TimeOfDay;
                var currentDay = DateTime.Now.DayOfWeek;

                var shiftResponses = shifts.Select(s => new ShiftResponseDto
                {
                    ShiftId = s.ShiftID,
                    Name = s.Name,
                    StartTime = s.StartTime.ToString(@"hh\:mm"),
                    EndTime = s.EndTime.ToString(@"hh\:mm"),
                    GracePeriodMinutes = s.GracePeriodMinutes,
                    DisplayTime = FormatShiftTime(s.StartTime, s.EndTime),
                    IsCurrentlyActive = IsShiftActive(s, currentTime, currentDay),
                    StatusColor = GetShiftStatusColor(s, currentTime, currentDay)
                }).ToList();

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Shifts retrieved successfully",
                    Data = shiftResponses
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shifts");
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "An error occurred while fetching shifts"
                });
            }
        }

        [HttpGet("shifts/{empCode}")]
        public async Task<ActionResult<ApiResponseDto>> GetEmployeeShifts(string empCode)
        {
            try
            {
                // Get all shifts for now (you can modify this to get employee-specific shifts if needed)
                var shifts = await _context.Shifts
                    .OrderBy(s => s.StartTime)
                    .ToListAsync();

                var currentTime = DateTime.Now.TimeOfDay;
                var currentDay = DateTime.Now.DayOfWeek;

                var shiftResponses = shifts.Select(s => new ShiftResponseDto
                {
                    ShiftId = s.ShiftID,
                    Name = s.Name,
                    StartTime = s.StartTime.ToString(@"hh\:mm"),
                    EndTime = s.EndTime.ToString(@"hh\:mm"),
                    GracePeriodMinutes = s.GracePeriodMinutes,
                    DisplayTime = FormatShiftTime(s.StartTime, s.EndTime),
                    IsCurrentlyActive = IsShiftActive(s, currentTime, currentDay),
                    StatusColor = GetShiftStatusColor(s, currentTime, currentDay)
                }).ToList();

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Employee shifts retrieved successfully",
                    Data = shiftResponses
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shifts for employee: {EmpCode}", empCode);
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "An error occurred while fetching employee shifts"
                });
            }
        }

        private bool IsShiftActive(Shift shift, TimeSpan currentTime, DayOfWeek currentDay)
        {
            var startTime = shift.StartTime;
            var endTime = shift.EndTime;

            // Handle overnight shifts
            if (endTime < startTime)
            {
                // Shift spans midnight
                return currentTime >= startTime || currentTime <= endTime;
            }
            else
            {
                // Normal shift
                return currentTime >= startTime && currentTime <= endTime;
            }
        }

        private string GetShiftStatusColor(Shift shift, TimeSpan currentTime, DayOfWeek currentDay)
        {
            var isActive = IsShiftActive(shift, currentTime, currentDay);
            
            if (isActive)
            {
                return "#38b000"; // Green for active
            }
            else if (shift.Name.Contains("Weekend", StringComparison.OrdinalIgnoreCase) ||
                    shift.Name.Contains("Sat", StringComparison.OrdinalIgnoreCase) ||
                    shift.Name.Contains("Sun", StringComparison.OrdinalIgnoreCase))
            {
                return "#FF9800"; // Orange for weekend
            }
            else
            {
                return "#666666"; // Gray for inactive
            }
        }

        private string FormatShiftTime(TimeSpan start, TimeSpan end)
        {
            var startTimeStr = DateTime.Today.Add(start).ToString("hh:mm tt");
            var endTimeStr = DateTime.Today.Add(end).ToString("hh:mm tt");
            return $"{startTimeStr} – {endTimeStr}";
        }

        private string FormatShiftTime(string startTimeStr, string endTimeStr)
        {
            if (TimeSpan.TryParse(startTimeStr, out var start) &&
                TimeSpan.TryParse(endTimeStr, out var end))
            {
                return FormatShiftTime(start, end);
            }

            return $"{startTimeStr} – {endTimeStr}";
        }
    }
}