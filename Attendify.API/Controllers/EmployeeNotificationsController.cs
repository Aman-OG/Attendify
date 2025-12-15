using Attendify.DATA;
using Attendify.DATA.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendify.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeNotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EmployeeNotificationsController> _logger;

        public EmployeeNotificationsController(AppDbContext context, ILogger<EmployeeNotificationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // DTOs
        public class NotificationResponseDto
        {
            public int MessageId { get; set; }
            public string Title { get; set; } = null!;
            public string Body { get; set; } = null!;
            public bool IsActive { get; set; }
            public string CreatedAt { get; set; } = null!;
            public string CreatedDate { get; set; } = null!;
            public string CreatedTime { get; set; } = null!;
            public string NotificationType { get; set; } = "Info";
            public string StatusBadge { get; set; } = "📢 Info";
            public string StatusColor { get; set; } = "#00A6FB";
            public string CardStyle { get; set; } = "Info";
        }

        public class ApiResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = null!;
            public object? Data { get; set; }
        }

        [HttpGet("messages")]
        public async Task<ActionResult<ApiResponseDto>> GetMessages()
        {
            try
            {
                var messages = await _context.AdminMessages
                    .Where(m => m.IsActive)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(20)
                    .ToListAsync();

                var notificationResponses = messages.Select(m => new NotificationResponseDto
                {
                    MessageId = m.MessageID,
                    Title = m.Title ?? "No Title",
                    Body = m.Body ?? "",
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    CreatedDate = m.CreatedAt.ToString("MMM dd, yyyy"),
                    CreatedTime = m.CreatedAt.ToString("hh:mm tt"),
                    NotificationType = DetermineNotificationType(m.Title, m.Body),
                    StatusBadge = GenerateStatusBadge(m.Title, m.Body, m.CreatedAt),
                    StatusColor = GetStatusColor(m.Title, m.Body, m.CreatedAt),
                    CardStyle = GetCardStyle(m.Title, m.Body)
                }).ToList();

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Messages retrieved successfully",
                    Data = notificationResponses
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin messages");
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "An error occurred while fetching messages"
                });
            }
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<ApiResponseDto>> GetUnreadCount()
        {
            try
            {

                var count = await _context.AdminMessages
                    .CountAsync(m => m.IsActive);

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Unread count retrieved successfully",
                    Data = new { Count = count }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "An error occurred while fetching unread count"
                });
            }
        }

        private string DetermineNotificationType(string title, string body)
        {
            if (string.IsNullOrEmpty(title))
                return "Info";

            var titleLower = title.ToLower();
            var bodyLower = body?.ToLower() ?? "";

            if (titleLower.Contains("emergency") || titleLower.Contains("urgent") ||
                titleLower.Contains("important") || titleLower.Contains("alert"))
                return "Important";

            if (titleLower.Contains("holiday") || titleLower.Contains("celebration") ||
                titleLower.Contains("congratulation"))
                return "Success";

            if (titleLower.Contains("maintenance") || titleLower.Contains("system") ||
                titleLower.Contains("update"))
                return "System";

            if (titleLower.Contains("meeting") || titleLower.Contains("training") ||
                titleLower.Contains("session"))
                return "Event";

            return "Info";
        }

        private string GenerateStatusBadge(string title, string body, DateTime createdAt)
        {
            var type = DetermineNotificationType(title, body);
            var now = DateTime.UtcNow;
            var daysOld = (now - createdAt).TotalDays;

            return type switch
            {
                "Important" => daysOld <= 1 ? "⚠️ Active" : "⚠️ Important",
                "Success" => "✅ Confirmed",
                "System" => "🔧 System",
                "Event" => "📅 Upcoming",
                _ => daysOld <= 1 ? "🆕 New" : "📢 Info"
            };
        }

        private string GetStatusColor(string title, string body, DateTime createdAt)
        {
            var type = DetermineNotificationType(title, body);

            return type switch
            {
                "Important" => "#FF6B6B",
                "Success" => "#2FBF4C",
                "System" => "#FF9800",
                "Event" => "#9C27B0",
                _ => "#00A6FB"
            };
        }

        private string GetCardStyle(string title, string body)
        {
            var type = DetermineNotificationType(title, body);

            return type switch
            {
                "Important" => "Important",
                "Success" => "Success",
                "System" => "Important", // System messages use Important style
                "Event" => "Info",
                _ => "Info"
            };
        }
    }
}