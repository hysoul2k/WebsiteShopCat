using Huy_Final_0843.Models;
using Microsoft.EntityFrameworkCore;

namespace Huy_Final_0843.Repositories
{
    public class EFCatRepository : ICatRepository
    {
        private readonly ApplicationDbContext _context;

        public EFCatRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Cat>> GetAllAsync()
            => await _context.Cats.AsNoTracking().ToListAsync();

        public async Task<Cat?> GetByIdAsync(int id)
            => await _context.Cats.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

        public async Task AddAsync(Cat cat)
        {
            _context.Cats.Add(cat);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Cat cat)
        {
            _context.Cats.Update(cat);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var cat = await _context.Cats.FindAsync(id);
            if (cat != null)
            {
                _context.Cats.Remove(cat);
                await _context.SaveChangesAsync();
            }
        }
    }
}
