using System.Security.Claims;
using Huy_Final_0843.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Huy_Final_0843.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Wishlist or /Wishlist/Index
        [HttpGet]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var wishlistItems = await _context.Wishlists
                .Include(w => w.Product)
                .ThenInclude(p => p.Images)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            return View(wishlistItems);
        }

        // GET: /Wishlist/Count
        [HttpGet("Count")]
        public async Task<IActionResult> Count()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { count = 0 });
            }

            var count = await _context.Wishlists.CountAsync(w => w.UserId == userId);
            return Json(new { count = count });
        }

        // POST: /Wishlist/Toggle/{productId}
        [HttpPost("Toggle/{productId}")]
        public async Task<IActionResult> Toggle(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { added = false, message = "Bạn cần đăng nhập để thêm vào yêu thích." });
            }

            var existingItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (existingItem == null)
            {
                var newItem = new Wishlist
                {
                    UserId = userId,
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Wishlists.Add(newItem);
                await _context.SaveChangesAsync();
                return Json(new { added = true, message = "Đã thêm vào danh sách yêu thích." });
            }
            else
            {
                _context.Wishlists.Remove(existingItem);
                await _context.SaveChangesAsync();
                return Json(new { added = false, message = "Đã xóa khỏi danh sách yêu thích." });
            }
        }

        // POST: /Wishlist/Remove/{id}
        [HttpPost("Remove/{id}")]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var item = await _context.Wishlists.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
            if (item != null)
            {
                _context.Wishlists.Remove(item);
                await _context.SaveChangesAsync();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true });
            }

            TempData["Success"] = "Đã xóa sản phẩm khỏi danh sách yêu thích.";
            return RedirectToAction(nameof(Index));
        }

        // Helper method
        public static bool IsInWishlist(ApplicationDbContext context, string userId, int productId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            return context.Wishlists.Any(w => w.UserId == userId && w.ProductId == productId);
        }
    }
}
