using Huy_Final_0843.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Huy_Final_0843.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("admin/chatlogs")]
    public class ChatLogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /admin/chatlogs
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        // GET: /admin/chatlogs/api
        [HttpGet("api")]
        public async Task<IActionResult> GetLogs([FromQuery] string? sessionId, [FromQuery] string? intent, [FromQuery] string? from, [FromQuery] string? to)
        {
            var query = _context.ChatLogs
                .Include(c => c.Account)
                .Where(c => c.MessageFrom == "user")
                .AsQueryable();

            if (!string.IsNullOrEmpty(sessionId))
                query = query.Where(c => c.SessionId.Contains(sessionId));

            if (!string.IsNullOrEmpty(intent))
                query = query.Where(c => c.Intent == intent);

            if (DateTime.TryParse(from, out var fromDate))
                query = query.Where(c => c.CreatedAt >= fromDate);

            if (DateTime.TryParse(to, out var toDate))
                query = query.Where(c => c.CreatedAt <= toDate.AddDays(1));

            var logs = await query
                .OrderByDescending(c => c.CreatedAt)
                .Take(200)
                .Select(c => new
                {
                    c.Id,
                    c.SessionId,
                    AccountEmail = c.Account != null ? c.Account.Email : null,
                    c.MessageContent,
                    c.Intent,
                    c.CreatedAt
                })
                .ToListAsync();

            return Ok(logs);
        }

        // GET: /admin/chatlogs/session/{sessionId}
        [HttpGet("session/{sessionId}")]
        public IActionResult Session(string sessionId)
        {
            ViewBag.SessionId = sessionId;
            return View();
        }

        // GET: /admin/chatlogs/session/{sessionId}/api
        [HttpGet("session/{sessionId}/api")]
        public async Task<IActionResult> GetSession(string sessionId)
        {
            var messages = await _context.ChatLogs
                .Include(c => c.Account)
                .Where(c => c.SessionId == sessionId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.MessageFrom,
                    c.MessageContent,
                    c.Intent,
                    c.CreatedAt,
                    AccountEmail = c.Account != null ? c.Account.Email : null
                })
                .ToListAsync();

            return Ok(messages);
        }
    }
}
