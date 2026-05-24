using Huy_Final_0843.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Huy_Final_0843.Controllers
{
    public class CatsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CatsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Details(int id)
        {
            var cat = await _context.Cats.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (cat == null) return NotFound();
            return View(cat);
        }
    }
}
