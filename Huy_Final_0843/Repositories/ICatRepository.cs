using Huy_Final_0843.Models;

namespace Huy_Final_0843.Repositories
{
    public interface ICatRepository
    {
        Task<IEnumerable<Cat>> GetAllAsync();
        Task<Cat?> GetByIdAsync(int id);
        Task AddAsync(Cat cat);
        Task UpdateAsync(Cat cat);
        Task DeleteAsync(int id);
    }
}
