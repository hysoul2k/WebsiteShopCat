using Huy_Final_0843.Models;
using Huy_Final_0843.ViewModels;
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

        public async Task<IActionResult> Index(string searchString, int? categoryId, decimal? minPrice, decimal? maxPrice, string sortOrder, string gender, int page = 1)
        {
            int pageSize = 12;
            var items = new List<ShopItemViewModel>();

            // categoryId=1 = Mèo Cảnh → chỉ Cats; null/0 = Tất cả → cả hai; khác → chỉ Products
            bool showCats     = !categoryId.HasValue || categoryId == 0 || categoryId == 1;
            bool showProducts = (!categoryId.HasValue || categoryId == 0) || categoryId > 1;

            if (showProducts && categoryId != 1)
            {
                var pq = _context.Products.AsNoTracking()
                    .Include(p => p.Category).Include(p => p.Reviews).AsQueryable();

                if (categoryId.HasValue && categoryId > 1)
                    pq = pq.Where(p => p.CategoryId == categoryId.Value);
                if (!string.IsNullOrEmpty(searchString))
                    pq = pq.Where(p => p.Name.Contains(searchString));
                if (minPrice.HasValue) pq = pq.Where(p => p.Price >= minPrice.Value);
                if (maxPrice.HasValue) pq = pq.Where(p => p.Price <= maxPrice.Value);

                var products = await pq.ToListAsync();
                items.AddRange(products.Select(p => new ShopItemViewModel
                {
                    Id            = p.Id,
                    Name          = p.Name,
                    Price         = p.Price,
                    ImageUrl      = p.ImageUrl,
                    IsCat         = false,
                    StockQuantity = p.StockQuantity,
                    AvgRating     = p.Reviews?.Any() == true ? p.Reviews.Average(r => r.Rating) : 0,
                    TotalReviews  = p.Reviews?.Count ?? 0,
                    CategoryName  = p.Category?.Name
                }));
            }

            if (showCats)
            {
                var cq = _context.Cats.AsNoTracking().AsQueryable();
                if (!string.IsNullOrEmpty(searchString))
                    cq = cq.Where(c => c.Name.Contains(searchString));
                if (minPrice.HasValue) cq = cq.Where(c => c.Price >= minPrice.Value);
                if (maxPrice.HasValue) cq = cq.Where(c => c.Price <= maxPrice.Value);

                if (!string.IsNullOrEmpty(gender))
                    cq = cq.Where(c => c.Gender == gender);

                var cats = await cq.ToListAsync();
                items.AddRange(cats.Select(c => new ShopItemViewModel
                {
                    Id           = c.Id,
                    Name         = c.Name,
                    Price        = c.Price,
                    ImageUrl     = c.ImageUrl,
                    IsCat        = true,
                    Gender       = c.Gender,
                    Age          = c.Age,
                    CategoryName = "Mèo Cảnh"
                }));
            }

            // Sắp xếp
            ViewBag.SortOrder = sortOrder;
            items = sortOrder switch
            {
                "price_asc"  => items.OrderBy(i => i.Price).ToList(),
                "price_desc" => items.OrderByDescending(i => i.Price).ToList(),
                "rating"     => items.OrderByDescending(i => i.AvgRating).ToList(),
                _            => items.OrderByDescending(i => i.Id).ToList()
            };

            int totalItems = items.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var paged = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage     = page;
            ViewBag.TotalPages      = totalPages;
            ViewBag.SearchString    = searchString;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.MinPrice        = minPrice;
            ViewBag.MaxPrice        = maxPrice;
            ViewBag.Gender          = gender;
            ViewBag.Categories      = await _context.Categories.ToListAsync();

            // Banner vẫn dùng Products
            var bannerProducts = await _context.Products.AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.StockQuantity > 0 && p.ImageUrl != null && p.ImageUrl != "")
                .GroupBy(p => p.CategoryId)
                .Select(g => g.OrderByDescending(p => p.Id).First())
                .Take(3).ToListAsync();

            if (bannerProducts.Count < 3)
                bannerProducts = await _context.Products.AsNoTracking()
                    .Include(p => p.Category)
                    .Where(p => p.ImageUrl != null && p.ImageUrl != "")
                    .OrderByDescending(p => p.Id).Take(3).ToListAsync();

            ViewBag.BannerProducts = bannerProducts;

            return View(paged);
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