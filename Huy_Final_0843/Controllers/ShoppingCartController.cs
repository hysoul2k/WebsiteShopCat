using Huy_Final_0843.Extensions;
using Huy_Final_0843.Models;
using Huy_Final_0843.Repositories;
using Huy_Final_0843.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Huy_Final_0843.Hubs;

namespace Huy_Final_0843.Controllers
{
    public class ShoppingCartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly IEmailSender _emailSender;

        public ShoppingCartController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IProductRepository productRepository,
            IHubContext<OrderHub> hubContext,
            IEmailSender emailSender)
        {
            _productRepository = productRepository;
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
            _emailSender = emailSender;
        }

        // 1. Hiển thị trang giỏ hàng
        [AllowAnonymous]
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            
            // Lấy thông tin Voucher nếu Session đang xài
            var appliedVoucher = HttpContext.Session.GetString("VoucherCode");
            var discountPercent = HttpContext.Session.GetInt32("DiscountPercent") ?? 0;
            
            ViewBag.AppliedVoucher = appliedVoucher;
            ViewBag.DiscountPercent = discountPercent;

            return View(cart);
        }

        // 2. Thêm sản phẩm vào giỏ
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) return NotFound();

            var cartItem = new CartItem
            {
                ProductId = productId,
                Name = product.Name,
                Price = product.Price,
                Quantity = quantity,
                ImageUrl = product.ImageUrl
            };

            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            cart.AddItem(cartItem);
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return RedirectToAction("Index");
        }

        // --- MỚI: AJAX ADD TO CART ---
        [HttpPost]
        public async Task<IActionResult> AddToCartAjax(int productId, int quantity = 1)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) return Json(new { success = false, message = "Sản phẩm không tồn tại!" });

            var cartItem = new CartItem
            {
                ProductId = productId,
                Name = product.Name,
                Price = product.Price,
                Quantity = quantity,
                ImageUrl = product.ImageUrl
            };

            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            cart.AddItem(cartItem);
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            // Tính tổng số lượng để update Badge Navbar
            int totalItems = cart.Items.Sum(i => i.Quantity);

            return Json(new { 
                success = true, 
                message = "Đã thêm vào giỏ hàng!", 
                productName = product.Name, 
                cartCount = totalItems 
            });
        }

        // 3. CẬP NHẬT: Tăng/Giảm số lượng và tự động tính lại giá
        // Tăng/Giảm số lượng sản phẩm trong giỏ
        public IActionResult UpdateQuantity(int productId, int delta)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart != null)
            {
                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (item != null)
                {
                    item.Quantity += delta;
                    // Nếu giảm xuống 0 thì xóa luôn món đó
                    if (item.Quantity <= 0)
                    {
                        cart.RemoveItem(productId);
                    }
                }
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }
            return RedirectToAction("Index");
        }

        // 4. Xóa hẳn sản phẩm khỏi giỏ
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart != null)
            {
                cart.RemoveItem(productId);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }
            return RedirectToAction("Index");
        }

        // --- CHỨC NĂNG THANH TOÁN (CHECKOUT) ---

        // Hiển thị form nhập địa chỉ
        [Authorize]
        public IActionResult Checkout()
        {
            return View(new Order());
        }

        // Xử lý lưu đơn hàng
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(order.ShippingAddress))
            {
                ModelState.AddModelError("ShippingAddress", "Vui lòng nhập địa chỉ giao hàng.");
                return View(order);
            }

            // KIỂM TRA LƯỢNG TỒN KHO LẦN CUỐI
            foreach (var item in cart.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null || product.StockQuantity < item.Quantity)
                {
                    ModelState.AddModelError("", $"Món hàng {item.Name} chỉ còn {product?.StockQuantity ?? 0} con/cái trong kho!");
                    return View(order);
                }
            }

            var user = await _userManager.GetUserAsync(User);
            order.UserId = user.Id;
            order.OrderDate = DateTime.UtcNow;
            order.Notes ??= ""; // <--- Bổ sung dòng này để tránh lỗi NULL trong Database
            order.PaymentMethod ??= "COD";
            
            
            // TÍNH TOÁN TIỀN VÀ KHẤU TRỪ VOUCHER
            decimal rootTotal = cart.Items.Sum(i => i.Price * i.Quantity);
            decimal finalTotal = rootTotal;

            var activeVoucherCode = HttpContext.Session.GetString("VoucherCode");
            if (!string.IsNullOrEmpty(activeVoucherCode))
            {
                var dbVoucher = _context.Vouchers.FirstOrDefault(v => v.Code == activeVoucherCode);
                if (dbVoucher != null && (dbVoucher.MaxUsage == 0 || dbVoucher.UsedCount < dbVoucher.MaxUsage) && dbVoucher.ExpiryDate >= DateTime.UtcNow.AddHours(7))
                {
                    decimal discountVal = rootTotal * ((decimal)dbVoucher.DiscountPercent / 100);
                    finalTotal = rootTotal - discountVal;

                    order.VoucherId = dbVoucher.Id;
                    order.DiscountAmount = discountVal;
                    
                    // Trừ luôn số lượt dùng của Voucher
                    dbVoucher.UsedCount += 1;
                    _context.Vouchers.Update(dbVoucher);
                }
            }

            order.TotalPrice = finalTotal;
            order.OrderDetails = cart.Items.Select(i => new OrderDetail
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList();

            // TRỪ KHO VẬT LÝ VÀ CHỐT ĐƠN
            foreach (var item in cart.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if(product != null)
                {
                    product.StockQuantity -= item.Quantity;
                    _context.Products.Update(product);
                }
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // --- SIGNALR: THÔNG BÁO CHO ADMIN ---
            string orderTime = DateTime.UtcNow.AddHours(7).ToString("HH:mm:ss");
            await _hubContext.Clients.All.SendAsync("ReceiveNewOrder", order.Id.ToString(), user.FullName ?? "Khách hàng", "Meow Garden", orderTime);

            // Xóa sạch giỏ hàng và Phiên Mã Giảm
            HttpContext.Session.Remove("Cart");
            HttpContext.Session.Remove("VoucherCode");
            HttpContext.Session.Remove("DiscountPercent");

            // --- EMAIL XÁC NHẬN ĐƠN HÀNG ---
            try
            {
                var viewOrderUrl = Url.Action("MyOrders", "Order", null, Request.Scheme) ?? "";
                var subject = $"Meow Garden - Xác nhận đơn hàng #{order.Id}";
                var body = EmailTemplateHelper.GetOrderConfirmationTemplate(order.Id, order.TotalPrice, order.PaymentMethod, viewOrderUrl);
                await _emailSender.SendEmailAsync(user.Email ?? "", subject, body);
            }
            catch { /* Email lỗi không chặn flow đặt hàng */ }

            if (order.PaymentMethod == "BankTransfer")
            {
                return RedirectToAction("PaymentQR", "Checkout", new { orderId = order.Id });
            }
            else
            {
                order.PaymentStatus = "COD";
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                return View("OrderCompleted", order.Id);
            }
        }

        // --- ĐỘNG CƠ MÃ GIẢM GIÁ (AJAX API) ---
        [HttpPost]
        [AllowAnonymous]
        public IActionResult ApplyVoucher([FromBody] string voucherCode)
        {
            if (string.IsNullOrWhiteSpace(voucherCode)) 
                return Json(new { success = false, message = "Vui lòng nhập mã" });

            var checkVoucher = _context.Vouchers.FirstOrDefault(v => v.Code == voucherCode.ToUpper());
            
            if (checkVoucher == null)
                return Json(new { success = false, message = "Mã không tồn tại!" });
            
            if (checkVoucher.ExpiryDate < DateTime.UtcNow)
                return Json(new { success = false, message = "Mã đã quá hạn sử dụng!" });
                
            if (checkVoucher.MaxUsage > 0 && checkVoucher.UsedCount >= checkVoucher.MaxUsage)
                return Json(new { success = false, message = "Mã đã hết lượt dùng!" });

            // Mã Hợp Lê -> Ghim Vào Ký Ức Hệ Thống (Session)
            HttpContext.Session.SetString("VoucherCode", checkVoucher.Code);
            HttpContext.Session.SetInt32("DiscountPercent", checkVoucher.DiscountPercent);

            return Json(new { 
                success = true, 
                message = "Áp mã thành công!", 
                percent = checkVoucher.DiscountPercent 
            });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAvailableVouchers()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            decimal cartTotal = cart.Items.Sum(i => i.Price * i.Quantity);
            DateTime now = DateTime.Now;

            var availableVouchers = _context.Vouchers
                .Where(v => v.IsActive
                    && v.ExpiryDate >= now
                    && (v.MaxUsage == 0 || v.UsedCount < v.MaxUsage)
                    && v.MinOrderAmount <= cartTotal)
                .Select(v => new
                {
                    code = v.Code,
                    description = $"Giảm {v.DiscountPercent}% cho đơn từ {v.MinOrderAmount:N0} vnđ", 
                    discountDisplay = v.DiscountType == "Percent"
                        ? $"{v.DiscountPercent}%"
                        : $"{v.DiscountValue}"
                })
                .ToList();

            return Json(availableVouchers);
        }
    }
}