using Huy_Final_0843.Models;
using Huy_Final_0843.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Huy_Final_0843.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Huy_Final_0843.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Staff)]
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ApplicationDbContext _db;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly Services.IAuditLogService _auditLogService;
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
        private readonly IMemoryCache _cache;

        public ProductController(IProductRepository productRepository,
                                 ICategoryRepository categoryRepository,
                                 ApplicationDbContext db,
                                 IHubContext<OrderHub> hubContext,
                                 Services.IAuditLogService auditLogService,
                                 Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager,
                                 IMemoryCache cache)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _db = db;
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
        public async Task<IActionResult> Add(Product product, IFormFile imageUrl, IFormFileCollection images)
        {
            if (ModelState.IsValid)
            {
                if (imageUrl != null)
                {
                    product.ImageUrl = await SaveImage(imageUrl);
                }

                await _productRepository.AddAsync(product);

                if (images != null && images.Any())
                {
                    var directory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                    Directory.CreateDirectory(directory);

                    var index = 0;
                    foreach (var file in images)
                    {
                        if (file == null || file.Length == 0)
                        {
                            continue;
                        }

                        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        var path = Path.Combine(directory, fileName);
                        using var stream = new FileStream(path, FileMode.Create);
                        await file.CopyToAsync(stream);

                        _db.ProductImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            Url = "/images/products/" + fileName,
                            IsPrimary = index == 0
                        });

                        index++;
                    }

                    await _db.SaveChangesAsync();
                }

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
        public async Task<IActionResult> Update(int id, Product product, IFormFile imageUrl, IFormFileCollection images)
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

                if (images != null && images.Any())
                {
                    var directory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                    Directory.CreateDirectory(directory);
                    foreach (var file in images)
                    {
                        if (file == null || file.Length == 0)
                        {
                            continue;
                        }

                        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        var path = Path.Combine(directory, fileName);
                        using var stream = new FileStream(path, FileMode.Create);
                        await file.CopyToAsync(stream);

                        _db.ProductImages.Add(new ProductImage
                        {
                            ProductId = existingProduct.Id,
                            Url = "/images/products/" + fileName,
                            IsPrimary = false
                        });
                    }
                    await _db.SaveChangesAsync();
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

        [HttpPost]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var image = await _db.ProductImages.FindAsync(imageId);
            if (image == null)
            {
                return Json(new { success = false });
            }

            var fileName = Path.GetFileName(image.Url);
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products", fileName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _db.ProductImages.Remove(image);
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> SetPrimaryImage(int imageId)
        {
            var image = await _db.ProductImages.FindAsync(imageId);
            if (image == null)
            {
                return Json(new { success = false, isPrimary = false });
            }

            var images = await _db.ProductImages
                .Where(pi => pi.ProductId == image.ProductId)
                .ToListAsync();

            foreach (var existingImage in images)
            {
                existingImage.IsPrimary = false;
            }

            image.IsPrimary = true;
            await _db.SaveChangesAsync();

            return Json(new { success = true, isPrimary = image.IsPrimary });
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