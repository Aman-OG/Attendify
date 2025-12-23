using Microsoft.AspNetCore.Mvc;

namespace Attendify.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemController : ControllerBase
    {
        [HttpGet("time")]
        public IActionResult GetServerTime()
        {
            return Ok(new
            {
                ServerTimeUtc = DateTime.UtcNow,
                LocalTime = DateTime.Now,
                TimeZone = TimeZoneInfo.Local.DisplayName
            });
        }
    }
}
