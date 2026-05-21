using Huy_Final_0843.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace Huy_Final_0843.Services
{
    /// <summary>
    /// Lightweight RAG service — retrieves product knowledge from DB
    /// and provides cat care knowledge for AI context injection.
    /// </summary>
    public class ProductKnowledgeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ProductKnowledgeService> _logger;

        private const string CATALOG_CACHE_KEY = "chat_product_catalog";
        private static readonly TimeSpan CATALOG_CACHE_DURATION = TimeSpan.FromMinutes(5);

        public ProductKnowledgeService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<ProductKnowledgeService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        // ══════════════════════════════════════════════════════
        // 1. FULL PRODUCT CATALOG (cached 5 min)
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Returns a formatted text summary of ALL products grouped by category.
        /// Cached for 5 minutes to avoid DB hits on every chat message.
        /// </summary>
        public async Task<string> GetProductCatalogAsync()
        {
            if (_cache.TryGetValue(CATALOG_CACHE_KEY, out string? cached) && cached != null)
                return cached;

            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.StockQuantity > 0)
                .OrderBy(p => p.CategoryId)
                .ThenBy(p => p.Price)
                .ToListAsync();

            if (!products.Any())
                return "[Chưa có sản phẩm trong hệ thống]";

            var sb = new StringBuilder();
            var grouped = products.GroupBy(p => p.Category?.Name ?? "Khác");

            foreach (var group in grouped)
            {
                sb.AppendLine($"\n📦 {group.Key}:");
                foreach (var p in group)
                {
                    var priceText = p.Price == 0 ? "Miễn phí" : $"{p.Price:N0}đ";
                    sb.AppendLine($"  • {p.Name} — {priceText}");
                    if (!string.IsNullOrWhiteSpace(p.Description))
                        sb.AppendLine($"    ↳ {p.Description}");
                }
            }

            var result = sb.ToString();
            _cache.Set(CATALOG_CACHE_KEY, result, CATALOG_CACHE_DURATION);
            _logger.LogInformation("[MeowChat] Product catalog cached: {Count} products", products.Count);
            return result;
        }

        // ══════════════════════════════════════════════════════
        // 2. KEYWORD-BASED PRODUCT SEARCH (lightweight RAG)
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Searches products relevant to the user's query using keyword matching.
        /// Returns top matches formatted for AI context.
        /// </summary>
        public async Task<string> SearchRelevantProductsAsync(string userQuery, int maxResults = 8)
        {
            if (string.IsNullOrWhiteSpace(userQuery))
                return "";

            var query = userQuery.ToLowerInvariant();
            var keywords = ExtractKeywords(query);

            if (!keywords.Any())
                return "";

            var allProducts = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.StockQuantity > 0)
                .ToListAsync();

            // Score each product by keyword match count
            var scored = allProducts.Select(p =>
            {
                var searchText = $"{p.Name} {p.Description} {p.Category?.Name}".ToLowerInvariant();
                int score = keywords.Sum(kw => searchText.Contains(kw) ? 1 : 0);
                return new { Product = p, Score = score };
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(maxResults)
            .ToList();

            if (!scored.Any())
                return "";

            var sb = new StringBuilder();
            sb.AppendLine("\n🔍 SẢN PHẨM LIÊN QUAN ĐẾN CÂU HỎI:");
            foreach (var item in scored)
            {
                var p = item.Product;
                var priceText = p.Price == 0 ? "Miễn phí" : $"{p.Price:N0}đ";
                sb.AppendLine($"  ★ {p.Name} — {priceText} (Còn {p.StockQuantity} sản phẩm)");
                if (!string.IsNullOrWhiteSpace(p.Description))
                    sb.AppendLine($"    ↳ {p.Description}");
            }

            _logger.LogInformation("[MeowChat] RAG matched {Count} products for query: {Query}",
                scored.Count, userQuery.Length > 80 ? userQuery[..80] : userQuery);

            return sb.ToString();
        }

        // ══════════════════════════════════════════════════════
        // 3. CAT KNOWLEDGE BASE (embedded)
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Returns embedded cat care knowledge for Health mode context.
        /// </summary>
        public string GetCatHealthKnowledge()
        {
            return @"
=== KIẾN THỨC CHĂM SÓC MÈO ===

📋 LỊCH TIÊM PHÒNG MÈO CON:
- 6-8 tuần tuổi: Mũi 1 (FVRCP — dại, viêm mũi, viêm phổi)
- 10-12 tuần tuổi: Mũi 2 (FVRCP nhắc lại)
- 14-16 tuần tuổi: Mũi 3 (FVRCP + Dại)
- Sau đó: Tiêm nhắc lại hàng năm

🍼 DINH DƯỠNG THEO ĐỘ TUỔI:
- Sơ sinh (0-4 tuần): Sữa mẹ hoặc sữa bột thay thế (Bio Milk)
- 1-3 tháng: Pate mềm + sữa bột, cho ăn 4-5 bữa/ngày
- 3-6 tháng: Hạt kitten (Royal Canin Kitten, Whiskas Kitten) + pate, 3-4 bữa/ngày
- 6-12 tháng: Hạt kitten, chuyển dần sang hạt adult, 2-3 bữa/ngày
- Trưởng thành (>12 tháng): Hạt adult, 2 bữa/ngày

⚠️ DẤU HIỆU CẦN ĐƯA ĐI BÁC SĨ THÚ Y NGAY:
- Bỏ ăn liên tục > 24 giờ
- Nôn mửa nhiều lần liên tiếp
- Tiêu chảy có máu
- Khó thở, thở nhanh bất thường
- Sốt cao (> 39.5°C)
- Co giật
- Không đi vệ sinh > 24 giờ
- Chảy dịch mũi/mắt liên tục
- Bụng trướng căng bất thường

🏥 BỆNH THƯỜNG GẶP Ở MÈO:
- Viêm đường hô hấp trên (hắt hơi, chảy nước mũi)
- Viêm ruột/tiêu chảy (thay đổi thức ăn đột ngột, ký sinh trùng)
- Nấm da (rụng lông từng mảng, ngứa)
- Viêm tai (gãi tai, lắc đầu, có mùi hôi)
- Sỏi thận/bàng quang (đi tiểu khó, tiểu ra máu)
- Búi lông trong ruột (nôn khan, táo bón)
- FIP (viêm phúc mạc truyền nhiễm) — nghiêm trọng
- FIV/FeLV (suy giảm miễn dịch) — nghiêm trọng

🐱 CHĂM SÓC CƠ BẢN:
- Vệ sinh khay cát: 1-2 lần/ngày
- Chải lông: 2-3 lần/tuần (lông dài: hàng ngày)
- Cắt móng: 2 tuần/lần
- Tắm: 1-2 tháng/lần (dùng sữa tắm chuyên dụng)
- Tẩy giun: 3 tháng/lần
- Nước uống: Thay nước sạch hàng ngày, khuyến khích dùng máy lọc nước
- Khám sức khỏe định kỳ: 6 tháng - 1 năm/lần";
        }

        /// <summary>
        /// Returns store context info for system prompt.
        /// </summary>
        public string GetStoreContext()
        {
            return @"
=== THÔNG TIN CỬA HÀNG ===
🏪 Tên: Meow Garden — Shop Mèo Cao Cấp
🎨 Phong cách: Earthy ấm áp, thân thiện
📦 Danh mục sản phẩm: Mèo Cảnh, Thức ăn cho Mèo, Dụng cụ & Phụ kiện
🚚 Hỗ trợ giao hàng toàn quốc
💳 Thanh toán: COD, Chuyển khoản (VietQR)
🎁 Có hệ thống voucher giảm giá
⭐ Có hệ thống đánh giá sản phẩm
❤️ Có tính năng Wishlist yêu thích";
        }

        // ══════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════

        private static List<string> ExtractKeywords(string query)
        {
            // Vietnamese keyword mapping for better matching
            var keywordMap = new Dictionary<string, string[]>
            {
                { "thức ăn", new[] { "hạt", "pate", "thức ăn", "ăn", "food" } },
                { "kitten", new[] { "kitten", "mèo con", "con nhỏ", "bé nhỏ", "sơ sinh" } },
                { "royal canin", new[] { "royal", "canin" } },
                { "whiskas", new[] { "whiskas" } },
                { "cát", new[] { "cát", "vệ sinh", "khay", "toilet" } },
                { "đồ chơi", new[] { "đồ chơi", "chơi", "cần câu", "laser", "toy" } },
                { "chải lông", new[] { "chải", "lược", "lông", "rụng lông" } },
                { "tắm", new[] { "tắm", "sữa tắm", "shampoo" } },
                { "vận chuyển", new[] { "balo", "túi", "vận chuyển", "di chuyển" } },
                { "bát ăn", new[] { "bát", "chén", "máy cho ăn", "nước" } },
                { "cào móng", new[] { "cào", "móng", "cat tree", "trụ" } },
                { "giống mèo", new[] { "giống", "aln", "scottish", "ba tư", "persian", "bengal",
                    "munchkin", "sphynx", "ragdoll", "siamese", "xiêm", "maine coon",
                    "russian blue", "nga", "mỹ", "abyssinian", "mướp", "exotic" } },
                { "sữa", new[] { "sữa", "milk", "bio milk" } },
                { "pate", new[] { "pate", "nekko", "snappy", "súp", "churu", "ciao" } },
                { "gel", new[] { "gel", "dinh dưỡng", "nutri" } },
                { "cỏ mèo", new[] { "cỏ", "catnip" } },
                { "vòng cổ", new[] { "vòng cổ", "chuông" } },
                { "freeze dried", new[] { "freeze", "sấy", "thịt sấy" } },
            };

            var words = query.Split(new[] { ' ', ',', '.', '?', '!' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var matched = new HashSet<string>();

            // Direct word matching
            foreach (var word in words)
            {
                if (word.Length >= 2)
                    matched.Add(word);
            }

            // Expand with related keywords
            foreach (var kvp in keywordMap)
            {
                if (kvp.Value.Any(kw => query.Contains(kw)))
                {
                    foreach (var expandedKw in kvp.Value)
                        matched.Add(expandedKw);
                }
            }

            return matched.ToList();
        }
    }
}
