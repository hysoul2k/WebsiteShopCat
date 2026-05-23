using Huy_Final_0843.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Huy_Final_0843.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Staff)]
    public class VoucherManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VoucherManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/VoucherManager/Index
        public async Task<IActionResult> Index()
        {
            var vouchers = await _context.Vouchers
                .OrderByDescending(v => v.ExpiryDate)
                .ToListAsync();

            return View(vouchers);
        }

        // GET: /Admin/VoucherManager/Create
        public IActionResult Create() => View(new Voucher());

        // POST: /Admin/VoucherManager/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Voucher voucher)
        {
            if (!ModelState.IsValid)
            {
                return View(voucher);
            }

            voucher.Code = voucher.Code?.Trim().ToUpperInvariant() ?? string.Empty;
            voucher.CreatedAt = DateTime.UtcNow;
            voucher.UsedCount = 0;

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tạo mã giảm giá thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/VoucherManager/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return NotFound();
            }

            return View(voucher);
        }

        // POST: /Admin/VoucherManager/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Voucher voucher)
        {
            if (id != voucher.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(voucher);
            }

            var existingVoucher = await _context.Vouchers.FindAsync(id);
            if (existingVoucher == null)
            {
                return NotFound();
            }

            existingVoucher.Code = voucher.Code?.Trim().ToUpperInvariant() ?? existingVoucher.Code;
            existingVoucher.DiscountType = voucher.DiscountType;
            existingVoucher.DiscountPercent = voucher.DiscountPercent;
            existingVoucher.MinOrderAmount = voucher.MinOrderAmount;
            existingVoucher.MaxUsage = voucher.MaxUsage;
            existingVoucher.UsedCount = voucher.UsedCount;
            existingVoucher.ExpiryDate = voucher.ExpiryDate;
            existingVoucher.IsActive = voucher.IsActive;

            _context.Vouchers.Update(existingVoucher);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/VoucherManager/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return Json(new { success = false });
            }

            // Nếu voucher đã được dùng trong order, xóa tham chiếu trước khi xóa voucher
            var relatedOrders = await _context.Orders
                .Where(o => o.VoucherId == id)
                .ToListAsync();

            if (relatedOrders.Any())
            {
                foreach (var order in relatedOrders)
                {
                    order.VoucherId = null;
                    _context.Orders.Update(order);
                }
            }

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: /Admin/VoucherManager/ToggleActive/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return Json(new { success = false, isActive = false });
            }

            voucher.IsActive = !voucher.IsActive;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = voucher.IsActive });
        }
    }
}
