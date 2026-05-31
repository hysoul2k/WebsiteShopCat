using Huy_Final_0843.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Caching.Memory;

namespace Huy_Final_0843.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("admin/faqs")]
    public class FaqManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public FaqManagerController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: /admin/faqs
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        // GET: /admin/faqs/api
        [HttpGet("api")]
        public async Task<IActionResult> GetFaqs([FromQuery] string? category, [FromQuery] bool? isActive)
        {
            var query = _context.Faqs.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(f => f.Category == category);
            }

            if (isActive.HasValue)
            {
                query = query.Where(f => f.IsActive == isActive.Value);
            }

            var faqs = await query.OrderByDescending(f => f.CreatedAt).ToListAsync();
            return Ok(faqs);
        }

        // POST: /admin/faqs/api
        [HttpPost("api")]
        public async Task<IActionResult> CreateFaq([FromBody] Faq faq)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            faq.CreatedAt = DateTime.UtcNow;
            faq.UpdatedAt = DateTime.UtcNow;

            _context.Faqs.Add(faq);
            await _context.SaveChangesAsync();
            _cache.Remove("ActiveFaqs");

            return Ok(faq);
        }

        // PUT: /admin/faqs/api/{id}
        [HttpPut("api/{id}")]
        public async Task<IActionResult> UpdateFaq(int id, [FromBody] Faq faq)
        {
            if (id != faq.FaqId) return BadRequest("ID mismatch");

            var existingFaq = await _context.Faqs.FindAsync(id);
            if (existingFaq == null) return NotFound();

            existingFaq.Question = faq.Question;
            existingFaq.Answer = faq.Answer;
            existingFaq.Category = faq.Category;
            existingFaq.IsActive = faq.IsActive;
            existingFaq.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _cache.Remove("ActiveFaqs");
            return Ok(existingFaq);
        }

        // DELETE: /admin/faqs/api/{id}
        [HttpDelete("api/{id}")]
        public async Task<IActionResult> DeleteFaq(int id)
        {
            var faq = await _context.Faqs.FindAsync(id);
            if (faq == null) return NotFound();

            _context.Faqs.Remove(faq);
            await _context.SaveChangesAsync();
            _cache.Remove("ActiveFaqs");

            return Ok(new { success = true });
        }

        // PATCH: /admin/faqs/api/{id}/toggle
        [HttpPatch("api/{id}/toggle")]
        public async Task<IActionResult> ToggleFaq(int id)
        {
            var faq = await _context.Faqs.FindAsync(id);
            if (faq == null) return NotFound();

            faq.IsActive = !faq.IsActive;
            faq.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _cache.Remove("ActiveFaqs");
            return Ok(faq);
        }
    }
}
