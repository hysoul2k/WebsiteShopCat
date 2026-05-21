using Huy_Final_0843.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Huy_Final_0843.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public CheckoutController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        // Cổng hiển thị QR Chuyển Khoản
        public async Task<IActionResult> PaymentQR(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null || order.UserId != user.Id)
            {
                return NotFound("Không tìm thấy đơn hàng, hoặc bạn không có quyền xem đơn hàng này.");
            }

            // Đọc biến từ Cấu hình AppSettings
            ViewBag.BankCode = _configuration["VietQR:BankCode"] ?? "MB";
            ViewBag.BankAccount = _configuration["VietQR:BankAccount"];
            ViewBag.AccountName = _configuration["VietQR:AccountName"];

            return View(order);
        }

        // Hành động chốt bấm "Tôi đã chuyển khoản"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmTransfer(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null || order.UserId != user.Id)
            {
                return NotFound("Đơn hàng không tồn tại.");
            }

            // Đổi trạng thái từ "Chờ xử lý" sang "Chờ Admin xác nhận" (Pending)
            order.Status = OrderStatus.Pending;
            order.PaymentStatus = "Pending";

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return View("PaymentSuccess", order.Id);
        }
    }
}
