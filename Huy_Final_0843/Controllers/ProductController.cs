using Huy_Final_0843.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Huy_Final_0843.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // --- CHI TIẾT SẢN PHẨM & LOAD REVIEWS ---
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews!) // Reviews! để ThenInclude hoạt động đúng
                    .ThenInclude(r => r.ApplicationUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound("Thú cưng không tồn tại!");
            }

            return View(product);
        }

        // --- SUBMIT BÌNH LUẬN ---
        [HttpPost]
        [Authorize] // Yêu cầu đăng nhập mới được viết bình luận
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostReview(int productId, int rating, string comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(comment))
            {
                TempData["Error"] = "Đánh giá không hợp lệ. Vui lòng đăng nhập và điền đủ thông tin!";
                return RedirectToAction("Details", new { id = productId });
            }

            var review = new Review
            {
                ProductId = productId,
                UserId = user.Id,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow.AddHours(7)
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đăng đánh giá thành công! Cảm ơn bạn đã tin dùng Meow Garden.";
            return RedirectToAction("Details", new { id = productId });
        }

        // --- MỚI: API GỢI Ý TÌM KIẾM (LIVE SEARCH) ---
        [HttpGet]
        public async Task<IActionResult> SearchSuggest(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new List<object>());
            }

            var suggestions = await _context.Products
                .AsNoTracking()
                .Where(p => p.Name.Contains(term))
                .OrderBy(p => p.Name)
                .Take(7)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price.ToString("N0"),
                    imageUrl = p.ImageUrl
                })
                .ToListAsync();

            return Json(suggestions);
        }
    }
}
