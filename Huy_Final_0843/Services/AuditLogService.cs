using Huy_Final_0843.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Huy_Final_0843.Services
{
    public interface IAuditLogService
    {
        Task LogActionAsync(string userId, string action, string tableName, string entityId, string details);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _context;

        public AuditLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogActionAsync(string userId, string action, string tableName, string entityId, string details)
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                TableName = tableName,
                EntityId = entityId,
                Details = details,
                Timestamp = DateTime.Now
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
