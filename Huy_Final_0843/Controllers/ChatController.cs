using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Huy_Final_0843.Services;
using Huy_Final_0843.Services.AI;
using Huy_Final_0843.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace Huy_Final_0843.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ChatController> _logger;
        private readonly ICatRagChatService _catRagChatService;

        // ══════════════════════════════════════════════════════════════
        // SYSTEM PROMPT — BOT 1: MEOWBOT (Mua bán & Tư vấn)
        // ══════════════════════════════════════════════════════════════
        private const string SHOP_SYSTEM_BASE = CatRagChatService.SHOP_SYSTEM_BASE;

        // ══════════════════════════════════════════════════════════════
        // SYSTEM PROMPT — BOT 2: DRPAWS (Sức khỏe mèo)
        // ══════════════════════════════════════════════════════════════
        private const string HEALTH_SYSTEM_BASE = CatRagChatService.HEALTH_SYSTEM_BASE;

        public ChatController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IMemoryCache cache,
            ApplicationDbContext db,
            ILogger<ChatController> logger,
            ICatRagChatService catRagChatService)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _cache = cache;
            _db = db;
            _logger = logger;
            _catRagChatService = catRagChatService;
        }

        // ══════════════════════════════════════════════════════════════
        // ENDPOINT CHÍNH (Send)
        // ══════════════════════════════════════════════════════════════
        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] ChatRequest request)
        {
            // ── Rate Limiting: 30 tin/giờ/IP ──────────────────────────
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var cacheKey = $"chat_rl_{ip}";
            var count = _cache.GetOrCreate(cacheKey, e => { e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1); return 0; });
            if (count >= 30)
                return StatusCode(429, new { error = "Bạn đã gửi quá nhiều tin nhắn. Thử lại sau 1 tiếng nhé! 😊" });
            _cache.Set(cacheKey, count + 1, TimeSpan.FromHours(1));

            // ── Validate ───────────────────────────────────────────────
            if (request?.Messages == null || request.Messages.Count == 0)
                return BadRequest(new { error = "Tin nhắn không hợp lệ." });

            try
            {
                var lastUserMessage = request.Messages.LastOrDefault(m => m.Role == "user")?.Content ?? "";
                _logger.LogInformation("[ChatController] Process Send request in Mode={Mode} | User query: '{Message}'", request.Mode, lastUserMessage);

                var response = await _catRagChatService.ProcessChatAsync(lastUserMessage, request.Mode, ip);
                return Ok(new { reply = response.Reply });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ChatController] Error processing send request.");
                return StatusCode(500, new { error = "Đã xảy ra lỗi.", detail = ex.Message });
            }
        }

        // ══════════════════════════════════════════════════════════════
        // ENDPOINT PHỤ (Chat - for Test backward compatibility)
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatInputModel input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.Message))
            {
                return BadRequest(new { error = "Message cannot be empty." });
            }

            try
            {
                var response = await _catRagChatService.ProcessChatAsync(input.Message, "shop", input.UserId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ChatController] Error processing chat: '{Message}'", input.Message);
                return StatusCode(500, new { error = "Đã xảy ra lỗi khi xử lý tin nhắn của bạn." });
            }
        }

        // ══════════════════════════════════════════════════════════════
        // QUERY DB — SẢN PHẨM CHO BOT MUA BÁN
        // ══════════════════════════════════════════════════════════════
        private async Task<string> GetProductsFromDb()
        {
            // Cache 10 phút để không query DB mỗi lần chat
            if (_cache.TryGetValue("db_products", out string? cached) && cached != null)
                return cached;

            var products = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.StockQuantity >= 0)
                .OrderBy(p => p.CategoryId)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Description,
                    p.StockQuantity,
                    CategoryName = p.Category != null ? p.Category.Name : "Khác",
                    AvgRating = p.Reviews != null && p.Reviews.Any()
                        ? Math.Round(p.Reviews.Average(r => r.Rating), 1)
                        : 0,
                    ReviewCount = p.Reviews != null ? p.Reviews.Count : 0
                })
                .ToListAsync();

            var sb = new StringBuilder();
            var grouped = products.GroupBy(p => p.CategoryName);

            foreach (var group in grouped)
            {
                sb.AppendLine($"\n[{group.Key.ToUpper()}]");
                foreach (var p in group)
                {
                    var stock = p.StockQuantity > 0 ? $"Còn {p.StockQuantity} hàng" : "⚠️ Hết hàng";
                    var rating = p.ReviewCount > 0 ? $"⭐{p.AvgRating}/5 ({p.ReviewCount} đánh giá)" : "Chưa có đánh giá";
                    sb.AppendLine($"- [{p.Id}] {p.Name}");
                    sb.AppendLine($"  Giá: {p.Price:N0}đ | {stock} | {rating}");
                    if (!string.IsNullOrWhiteSpace(p.Description))
                        sb.AppendLine($"  Mô tả: {p.Description.Substring(0, Math.Min(150, p.Description.Length))}...");
                }
            }

            var result = sb.ToString();
            _cache.Set("db_products", result, TimeSpan.FromMinutes(10));
            return result;
        }

        // ══════════════════════════════════════════════════════════════
        // QUERY DB — SẢN PHẨM CHO BOT SỨC KHỎE
        // (Lọc các sản phẩm liên quan sức khỏe: thức ăn, vệ sinh, chăm sóc)
        // ══════════════════════════════════════════════════════════════
        private async Task<string> GetHealthProductsFromDb()
        {
            if (_cache.TryGetValue("db_health_products", out string? cached) && cached != null)
                return cached;

            var healthKeywords = new[] { "thức ăn", "food", "vệ sinh", "hygiene", "chăm sóc", "care", "sức khỏe", "health", "sữa", "vitamin" };

            var products = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.StockQuantity > 0 &&
                    p.Category != null &&
                    healthKeywords.Any(k => p.Category.Name.ToLower().Contains(k) ||
                                           p.Name.ToLower().Contains(k)))
                .Select(p => new
                {
                    p.Name,
                    p.Price,
                    p.Description,
                    p.StockQuantity,
                    CategoryName = p.Category != null ? p.Category.Name : "Khác"
                })
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Sản phẩm liên quan sức khỏe hiện có tại shop:");
            foreach (var p in products)
            {
                sb.AppendLine($"- {p.Name} ({p.CategoryName}) — {p.Price:N0}đ — Còn {p.StockQuantity} hàng");
                if (!string.IsNullOrWhiteSpace(p.Description))
                    sb.AppendLine($"  {p.Description.Substring(0, Math.Min(100, p.Description.Length))}");
            }

            if (!products.Any())
                sb.AppendLine("(Hiện chưa có sản phẩm sức khỏe trong kho)");

            var result = sb.ToString();
            _cache.Set("db_health_products", result, TimeSpan.FromMinutes(10));
            return result;
        }

        // ══════════════════════════════════════════════════════════════
        // ENDPOINT PHỤ: Xóa cache sản phẩm khi admin cập nhật hàng
        // ══════════════════════════════════════════════════════════════
        [HttpPost("clear-cache")]
        public IActionResult ClearProductCache()
        {
            _cache.Remove("db_products");
            _cache.Remove("db_health_products");
            return Ok(new { message = "Cache đã được xóa, bot sẽ đọc dữ liệu mới." });
        }
    }

    // ══════════════════════════════════════════════════════════════
    // REQUEST/RESPONSE MODELS
    // ══════════════════════════════════════════════════════════════
    public class ChatRequest
    {
        public string Mode { get; set; } = "shop"; // "shop" | "health"
        public List<ChatMessage> Messages { get; set; } = new();
    }

    public class ChatMessage
    {
        public string Role { get; set; } = "user"; // "user" | "assistant"
        public string Content { get; set; } = "";
    }

    public class ChatInputModel
    {
        public string Message { get; set; } = "";
        public string? UserId { get; set; }
    }
}
