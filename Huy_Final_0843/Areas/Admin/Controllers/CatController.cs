using Huy_Final_0843.Models;
using Huy_Final_0843.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Huy_Final_0843.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Staff)]
    public class CatController : Controller
    {
        private readonly ICatRepository _catRepository;
        private readonly Services.IAuditLogService _auditLogService;
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
        private readonly IMemoryCache _cache;

        public CatController(ICatRepository catRepository,
                             Services.IAuditLogService auditLogService,
                             Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager,
                             IMemoryCache cache)
        {
            _catRepository = catRepository;
            _auditLogService = auditLogService;
            _userManager = userManager;
            _cache = cache;
        }

        public async Task<IActionResult> Index()
        {
            var cats = await _catRepository.GetAllAsync();
            return View(cats);
        }

        public IActionResult Add() => View();

        [HttpPost]
        public async Task<IActionResult> Add(Cat cat, IFormFile? imageUrl)
        {
            ModelState.Remove("ImageUrl");
            if (!ModelState.IsValid) return View(cat);

            if (imageUrl != null)
                cat.ImageUrl = await SaveImage(imageUrl);

            await _catRepository.AddAsync(cat);
            _cache.Remove("db_products");

            var user = await _userManager.GetUserAsync(User);
            await _auditLogService.LogActionAsync(user!.Id, "Thêm mèo", "Cats", cat.Id.ToString(), $"Tên: {cat.Name}, Giới tính: {cat.Gender}, Tuổi: {cat.Age}");

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Update(int id)
        {
            var cat = await _catRepository.GetByIdAsync(id);
            if (cat == null) return NotFound();
            return View(cat);
        }

        [HttpPost]
        public async Task<IActionResult> Update(int id, Cat cat, IFormFile? imageUrl)
        {
            ModelState.Remove("ImageUrl");
            if (id != cat.Id) return NotFound();
            if (!ModelState.IsValid) return View(cat);

            var existing = await _catRepository.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.Name        = cat.Name;
            existing.Price       = cat.Price;
            existing.Description = cat.Description;
            existing.Gender      = cat.Gender;
            existing.Age         = cat.Age;
            existing.ImageUrl    = imageUrl != null ? await SaveImage(imageUrl) : existing.ImageUrl;

            await _catRepository.UpdateAsync(existing);
            _cache.Remove("db_products");

            var user = await _userManager.GetUserAsync(User);
            await _auditLogService.LogActionAsync(user!.Id, "Cập nhật mèo", "Cats", id.ToString(), $"Tên: {existing.Name}, Giới tính: {existing.Gender}");

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var cat = await _catRepository.GetByIdAsync(id);
            if (cat == null) return NotFound();
            return View(cat);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cat = await _catRepository.GetByIdAsync(id);
            var name = cat?.Name ?? "N/A";
            await _catRepository.DeleteAsync(id);
            _cache.Remove("db_products");

            var user = await _userManager.GetUserAsync(User);
            await _auditLogService.LogActionAsync(user!.Id, "Xóa mèo", "Cats", id.ToString(), $"Tên đã xóa: {name}");

            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveImage(IFormFile image)
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "cats");
            Directory.CreateDirectory(dir);
            var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
            var path = Path.Combine(dir, fileName);
            using var stream = new FileStream(path, FileMode.Create);
            await image.CopyToAsync(stream);
            return "/images/cats/" + fileName;
        }
    }
}
