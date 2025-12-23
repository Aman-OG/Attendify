using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Attendify.DATA;
using Attendify.DATA.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Attendify.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SettingsController(AppDbContext context)
        {
            _context = context;
        }

        #region Attendance Rules Endpoints

        // GET: api/settings/attendance-rules
        [HttpGet("attendance-rules")]
        public async Task<ActionResult<IEnumerable<object>>> GetAttendanceRules()
        {
            try
            {
                var rules = await _context.AttendanceRules
                    .Select(r => new
                    {
                        r.RuleID,
                        Day = r.DayOfWeek, // Map DayOfWeek to Day for WPF
                        StartTime = r.StartTime.ToString(@"hh\:mm"), // Format for WPF
                        EndTime = r.EndTime.ToString(@"hh\:mm"),
                        GracePeriod = r.GracePeriodMinutes.ToString() // Convert to string for WPF
                    })
                    .ToListAsync();
                return Ok(rules);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST: api/settings/attendance-rules
        [HttpPost("attendance-rules")]
        public async Task<ActionResult<object>> CreateAttendanceRule([FromBody] AttendanceRuleDto ruleDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var rule = new AttendanceRule
                {
                    DayOfWeek = ruleDto.Day,
                    StartTime = TimeSpan.Parse(ruleDto.StartTime),
                    EndTime = TimeSpan.Parse(ruleDto.EndTime),
                    GracePeriodMinutes = int.TryParse(ruleDto.GracePeriod, out int grace) ? grace : 10,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AttendanceRules.Add(rule);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    AttendanceRuleId = rule.RuleID,
                    rule.DayOfWeek,
                    rule.StartTime,
                    rule.EndTime,
                    GracePeriod = rule.GracePeriodMinutes.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/settings/attendance-rules/{id}
        [HttpPut("attendance-rules/{id}")]
        public async Task<IActionResult> UpdateAttendanceRule(int id, [FromBody] AttendanceRuleDto ruleDto)
        {
            try
            {
                var existingRule = await _context.AttendanceRules.FindAsync(id);
                if (existingRule == null)
                {
                    return NotFound();
                }

                existingRule.DayOfWeek = ruleDto.Day;
                existingRule.StartTime = TimeSpan.Parse(ruleDto.StartTime);
                existingRule.EndTime = TimeSpan.Parse(ruleDto.EndTime);
                existingRule.GracePeriodMinutes = int.TryParse(ruleDto.GracePeriod, out int grace) ? grace : 10;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DELETE: api/settings/attendance-rules/{id}
        [HttpDelete("attendance-rules/{id}")]
        public async Task<IActionResult> DeleteAttendanceRule(int id)
        {
            try
            {
                var rule = await _context.AttendanceRules.FindAsync(id);
                if (rule == null)
                {
                    return NotFound();
                }

                _context.AttendanceRules.Remove(rule);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        #endregion

        #region Shifts Endpoints

        // GET: api/settings/shifts
        [HttpGet("shifts")]
        public async Task<ActionResult<IEnumerable<object>>> GetShifts()
        {
            try
            {
                var shifts = await _context.Shifts
                    .Select(s => new
                    {
                        ShiftId = s.ShiftID,
                        s.Name,
                        StartTime = s.StartTime.ToString(@"hh\:mm"),
                        EndTime = s.EndTime.ToString(@"hh\:mm"),
                        GracePeriod = s.GracePeriodMinutes.ToString()
                    })
                    .ToListAsync();
                return Ok(shifts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST: api/settings/shifts
        [HttpPost("shifts")]
        public async Task<ActionResult<object>> CreateShift([FromBody] ShiftDto shiftDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var shift = new Shift
                {
                    Name = shiftDto.Name,
                    StartTime = TimeSpan.Parse(shiftDto.StartTime),
                    EndTime = TimeSpan.Parse(shiftDto.EndTime),
                    GracePeriodMinutes = int.TryParse(shiftDto.GracePeriod, out int grace) ? grace : 5,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Shifts.Add(shift);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    ShiftId = shift.ShiftID,
                    shift.Name,
                    shift.StartTime,
                    shift.EndTime,
                    GracePeriod = shift.GracePeriodMinutes.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/settings/shifts/{id}
        [HttpPut("shifts/{id}")]
        public async Task<IActionResult> UpdateShift(int id, [FromBody] ShiftDto shiftDto)
        {
            try
            {
                var existingShift = await _context.Shifts.FindAsync(id);
                if (existingShift == null)
                {
                    return NotFound();
                }

                existingShift.Name = shiftDto.Name;
                existingShift.StartTime = TimeSpan.Parse(shiftDto.StartTime);
                existingShift.EndTime = TimeSpan.Parse(shiftDto.EndTime);
                existingShift.GracePeriodMinutes = int.TryParse(shiftDto.GracePeriod, out int grace) ? grace : 5;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DELETE: api/settings/shifts/{id}
        [HttpDelete("shifts/{id}")]
        public async Task<IActionResult> DeleteShift(int id)
        {
            try
            {
                var shift = await _context.Shifts.FindAsync(id);
                if (shift == null)
                {
                    return NotFound();
                }

                _context.Shifts.Remove(shift);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        #endregion

        #region Broadcast Messages Endpoints

        // GET: api/settings/broadcast-messages
        [HttpGet("broadcast-messages")]
        public async Task<ActionResult<IEnumerable<object>>> GetBroadcastMessages()
        {
            try
            {
                var messages = await _context.AdminMessages
                    .Select(m => new
                    {
                        BroadcastMessageId = m.MessageID,
                        m.Title,
                        m.Body,
                        Status = m.IsActive ? "Active" : "Inactive",
                        StatusColor = m.IsActive ? "#4CAF50" : "#F44336",
                        CreatedDate = m.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                    })
                    .ToListAsync();
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST: api/settings/broadcast-messages
        [HttpPost("broadcast-messages")]
        public async Task<ActionResult<object>> CreateBroadcastMessage([FromBody] BroadcastMessageDto messageDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var message = new AdminMessage
                {
                    Title = messageDto.Title,
                    Body = messageDto.Body,
                    Type = "Broadcast",
                    IsActive = messageDto.Status == "Active",
                    CreatedAt = DateTime.UtcNow
                };

                _context.AdminMessages.Add(message);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    BroadcastMessageId = message.MessageID,
                    message.Title,
                    message.Body,
                    Status = message.IsActive ? "Active" : "Inactive",
                    StatusColor = message.IsActive ? "#4CAF50" : "#F44336",
                    CreatedDate = message.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/settings/broadcast-messages/{id}
        [HttpPut("broadcast-messages/{id}")]
        public async Task<IActionResult> UpdateBroadcastMessage(int id, [FromBody] BroadcastMessageDto messageDto)
        {
            try
            {
                var existingMessage = await _context.AdminMessages.FindAsync(id);
                if (existingMessage == null)
                {
                    return NotFound();
                }

                existingMessage.Title = messageDto.Title;
                existingMessage.Body = messageDto.Body;
                existingMessage.IsActive = messageDto.Status == "Active";

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DELETE: api/settings/broadcast-messages/{id}
        [HttpDelete("broadcast-messages/{id}")]
        public async Task<IActionResult> DeleteBroadcastMessage(int id)
        {
            try
            {
                var message = await _context.AdminMessages.FindAsync(id);
                if (message == null)
                {
                    return NotFound();
                }

                _context.AdminMessages.Remove(message);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        #endregion

        #region Employee Requests Endpoints

        // GET: api/settings/employee-requests
        [HttpGet("employee-requests")]
        public async Task<ActionResult<IEnumerable<object>>> GetEmployeeRequests([FromQuery] string? search = null)
        {
            try
            {
                var query = _context.EmployeeRequests
                    .Include(er => er.Employee)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.ToLower();
                    query = query.Where(r =>
                        r.Employee.EmpCode.ToLower().Contains(term) ||
                        (r.Employee.FirstName + " " + r.Employee.LastName).ToLower().Contains(term) ||
                        r.Type.ToLower().Contains(term));
                }

                var requests = await query
                    .Select(r => new
                    {
                        EmployeeRequestId = r.RequestID,
                        EmployeeID = r.Employee != null ? r.Employee.EmpCode : "GUEST",
                        EmployeeName = r.Employee != null 
                            ? (r.Employee.FirstName + (string.IsNullOrEmpty(r.Employee.MiddleName) ? "" : " " + r.Employee.MiddleName)) 
                            : "Unknown User",
                        r.Type,
                        r.Message,
                        r.Status,
                        StatusColor = r.Status == "Pending" ? "#FF9800" :
                                    r.Status == "Approved" ? "#4CAF50" :
                                    r.Status == "Rejected" ? "#F44336" : "#9E9E9E",
                        CreatedDate = r.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                    })
                    .ToListAsync();

                return Ok(requests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/settings/employee-requests/{id}/review
        [HttpPut("employee-requests/{id}/review")]
        public async Task<IActionResult> ReviewEmployeeRequest(int id, [FromBody] ReviewRequestDto reviewDto)
        {
            try
            {
                var request = await _context.EmployeeRequests.FindAsync(id);
                if (request == null)
                {
                    return NotFound();
                }

                request.Status = reviewDto.Decision;
                request.AdminReply = reviewDto.AdminReply;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Request reviewed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        #endregion

        #region DTO Classes

        public class AttendanceRuleDto
        {
            public string Day { get; set; } = "";
            public string StartTime { get; set; } = "";
            public string EndTime { get; set; } = "";
            public string GracePeriod { get; set; } = "";
        }

        public class ShiftDto
        {
            public string Name { get; set; } = "";
            public string StartTime { get; set; } = "";
            public string EndTime { get; set; } = "";
            public string GracePeriod { get; set; } = "";
        }

        public class BroadcastMessageDto
        {
            public string Title { get; set; } = "";
            public string Body { get; set; } = "";
            public string Status { get; set; } = "Active";
        }

        public class ReviewRequestDto
        {
            public string Decision { get; set; } = ""; // "Approved" or "Rejected"
            public string AdminReply { get; set; } = "";
            public int AdminId { get; set; }
        }

        #endregion
    }
}