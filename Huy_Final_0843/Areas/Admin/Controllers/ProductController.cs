using Huy_Final_0843.Models;
using Huy_Final_0843.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Huy_Final_0843.Hubs;

using Microsoft.Extensions.Caching.Memory;

namespace Huy_Final_0843.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Staff)]
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly Services.IAuditLogService _auditLogService;
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
        private readonly IMemoryCache _cache;

        public ProductController(IProductRepository productRepository,
                                 ICategoryRepository categoryRepository,
                                 IHubContext<OrderHub> hubContext,
                                 Services.IAuditLogService auditLogService,
                                 Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager,
                                 IMemoryCache cache)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _hubContext = hubContext;
            _auditLogService = auditLogService;
            _userManager = userManager;
            _cache = cache;
        }

        // Hiển thị danh sách sản phẩm
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

        // Hiển thị form thêm sản phẩm
        public async Task<IActionResult> Add()
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        // Xử lý thêm sản phẩm
        [HttpPost]
        public async Task<IActionResult> Add(Product product, IFormFile imageUrl)
        {
            if (ModelState.IsValid)
            {
                if (imageUrl != null)
                {
                    product.ImageUrl = await SaveImage(imageUrl);
                }

                await _productRepository.AddAsync(product);

                // Clear Cache
                _cache.Remove("db_products");
                _cache.Remove("db_health_products");

                // Ghi Log
                var user = await _userManager.GetUserAsync(User);
                await _auditLogService.LogActionAsync(user.Id, "Thêm mới sản phẩm", "Products", product.Id.ToString(), $"Tên: {product.Name}, Giá: {product.Price}");

                return RedirectToAction(nameof(Index));
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");

            return View(product);
        }

        // Hiển thị thông tin chi tiết sản phẩm
        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // Hiển thị form cập nhật sản phẩm
        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);

            return View(product);
        }

        // Xử lý cập nhật sản phẩm
        [HttpPost]
        public async Task<IActionResult> Update(int id, Product product, IFormFile imageUrl)
        {
            ModelState.Remove("ImageUrl");

            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingProduct = await _productRepository.GetByIdAsync(id);

                if (imageUrl == null)
                {
                    product.ImageUrl = existingProduct.ImageUrl;
                }
                else
                {
                    product.ImageUrl = await SaveImage(imageUrl);
                }

                existingProduct.Name = product.Name;
                existingProduct.Price = product.Price;
                existingProduct.Description = product.Description;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.ImageUrl = product.ImageUrl;
                existingProduct.StockQuantity = product.StockQuantity;

                await _productRepository.UpdateAsync(existingProduct);

                // Clear Cache
                _cache.Remove("db_products");
                _cache.Remove("db_health_products");

                // Ghi Log
                var user = await _userManager.GetUserAsync(User);
                await _auditLogService.LogActionAsync(user.Id, "Cập nhật sản phẩm", "Products", id.ToString(), $"Tên mới: {existingProduct.Name}, Giá mới: {existingProduct.Price}, Kho mới: {existingProduct.StockQuantity}");

                // SIGNALR: Thông báo cập nhật tồn kho Real-time
                await _hubContext.Clients.All.SendAsync("UpdateStock", id.ToString(), existingProduct.StockQuantity);

                return RedirectToAction(nameof(Index));
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");

            return View(product);
        }

        // Hiển thị form xác nhận xóa
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // Xử lý xóa sản phẩm
        [HttpPost, ActionName("DeleteConfirmed")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            var productName = product?.Name ?? "N/A";

            await _productRepository.DeleteAsync(id);

            // Clear Cache
            _cache.Remove("db_products");
            _cache.Remove("db_health_products");

            // Ghi Log
            var user = await _userManager.GetUserAsync(User);
            await _auditLogService.LogActionAsync(user.Id, "Xóa sản phẩm", "Products", id.ToString(), $"Tên sản phẩm đã xóa: {productName}");

            return RedirectToAction(nameof(Index));
        }

        // Hàm lưu hình ảnh
        private async Task<string> SaveImage(IFormFile image)
        {
            var savePath = Path.Combine("wwwroot/images", image.FileName);

            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return "/images/" + image.FileName;
        }
    }
}