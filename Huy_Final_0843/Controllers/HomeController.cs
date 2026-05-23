using Huy_Final_0843.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Huy_Final_0843.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // 3. Cập nhật Action Index với Phân trang, Lọc & Sắp xếp
        public async Task<IActionResult> Index(string searchString, int? categoryId, decimal? minPrice, decimal? maxPrice, string sortOrder, int page = 1)
        {
            int pageSize = 12;

            // Khởi tạo truy vấn gốc
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .AsQueryable();

            // LỌC THEO TÊN
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.Name.Contains(searchString));
            }

            // LỌC THEO DANH MỤC
            if (categoryId.HasValue && categoryId > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // LỌC THEO KHOẢNG GIÁ
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            // SẮP XẾP
            ViewBag.SortOrder = sortOrder;
            query = sortOrder switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "rating" => query.OrderByDescending(p => p.Reviews != null && p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0),
                _ => query.OrderByDescending(p => p.Id)
            };

            // Lấy tổng số kết quả
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Truyền dữ liệu qua ViewBag
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchString = searchString;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            
            ViewBag.Categories = await _context.Categories.ToListAsync();

            // Banner: 1 sản phẩm nổi bật mỗi danh mục, ưu tiên có ảnh và còn hàng
            var bannerProducts = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.StockQuantity > 0 && p.ImageUrl != null && p.ImageUrl != "")
                .GroupBy(p => p.CategoryId)
                .Select(g => g.OrderByDescending(p => p.Id).First())
                .Take(3)
                .ToListAsync();

            if (bannerProducts.Count < 3)
            {
                // Fallback nếu ít hơn 3 category
                var fallback = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Where(p => p.ImageUrl != null && p.ImageUrl != "")
                    .OrderByDescending(p => p.Id)
                    .Take(3)
                    .ToListAsync();
                bannerProducts = fallback;
            }

            ViewBag.BannerProducts = bannerProducts;

            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}