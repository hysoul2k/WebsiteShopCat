using Huy_Final_0843.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Huy_Final_0843.Hubs;
using Microsoft.AspNetCore.Identity.UI.Services;
using Huy_Final_0843.Helpers;

namespace Huy_Final_0843.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Staff)]
    public class OrderManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly IEmailSender _emailSender;

        public OrderManagerController(ApplicationDbContext context, IHubContext<OrderHub> hubContext, IEmailSender emailSender)
        {
            _context = context;
            _hubContext = hubContext;
            _emailSender = emailSender;
        }

        // Lấy danh sách giao dịch đơn hàng
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.ApplicationUser)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        // Xem chi tiết đơn hàng
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status, string? reason, bool returnToDetails = false)
        {
            var order = await _context.Orders.Include(o => o.ApplicationUser).FirstOrDefaultAsync(o => o.Id == id);
            if (order != null)
            {
                order.Status = status;
                if (status == OrderStatus.Cancelled)
                {
                    order.CancellationReason = reason;
                }
                await _context.SaveChangesAsync();

                string statusText = status switch
                {
                    OrderStatus.Pending => "Chờ xử lý",
                    OrderStatus.Shipping => "Đang giao hàng",
                    OrderStatus.Completed => "Hoàn thành",
                    OrderStatus.Cancelled => "Đã hủy",
                    _ => status.ToString()
                };

                if (status == OrderStatus.Shipping || status == OrderStatus.Completed)
                {
                    var viewOrderUrl = Url.Action("MyOrders", "Order", new { area = "" }, Request.Scheme) ?? "";
                    var subject = $"Meow Garden - Cập nhật đơn hàng #{order.Id}";
                    var message = EmailTemplateHelper.GetOrderUpdateTemplate(order.Id, order.TotalPrice, statusText, viewOrderUrl);
                    await _emailSender.SendEmailAsync(order.ApplicationUser?.Email ?? "", subject, message);
                }

                await _hubContext.Clients.User(order.UserId).SendAsync("ReceiveStatusUpdate", order.Id.ToString(), statusText, "Ban quản trị Meow Garden");
            }
            if (returnToDetails)
            {
                return RedirectToAction(nameof(Details), new { id = id });
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus status, string? cancellationReason)
        {
            var order = await _context.Orders.Include(o => o.ApplicationUser).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order != null)
            {
                order.Status = status;
                if (status == OrderStatus.Cancelled)
                {
                    order.CancellationReason = cancellationReason;
                }
                
                await _context.SaveChangesAsync();

                string statusText = status switch
                {
                    OrderStatus.Pending => "Chờ xử lý",
                    OrderStatus.Shipping => "Đang giao hàng",
                    OrderStatus.Completed => "Hoàn thành",
                    OrderStatus.Cancelled => "Đã hủy",
                    _ => status.ToString()
                };

                if (status == OrderStatus.Shipping || status == OrderStatus.Completed)
                {
                    var viewOrderUrl = Url.Action("MyOrders", "Order", new { area = "" }, Request.Scheme) ?? "";
                    var subject = $"Meow Garden - Cập nhật đơn hàng #{order.Id}";
                    var message = EmailTemplateHelper.GetOrderUpdateTemplate(order.Id, order.TotalPrice, statusText, viewOrderUrl);
                    await _emailSender.SendEmailAsync(order.ApplicationUser?.Email ?? "", subject, message);
                }

                await _hubContext.Clients.User(order.UserId).SendAsync("ReceiveStatusUpdate", order.Id.ToString(), statusText, "Ban quản trị Meow Garden");

                TempData["Message"] = "Cập nhật trạng thái đơn hàng #" + orderId + " thành [" + statusText + "] thành công! - Ban quản trị Meow Garden";
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAllOrders()
        {
            try
            {
                // 1. Xóa chi tiết đơn hàng trước
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM OrderDetails");
                
                // 2. Xóa đơn hàng
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Orders");

                // 3. Reset ID về 1 (RESEED, 0 nghĩa là cái tiếp theo sẽ là 1)
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Orders', RESEED, 0)");
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('OrderDetails', RESEED, 0)");

                // 4. SIGNALR: Thông báo cho tất cả Admin reload lại trang
                await _hubContext.Clients.All.SendAsync("RefreshOrders");

                TempData["Success"] = "Hệ thống đã dọn dẹp sạch dữ liệu đơn hàng!";
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi dọn dẹp: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
