using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Infrastructure.PostgreSQL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LinkGuardiao.Infrastructure.PostgreSQL.Repositories
{
    public class EfAccessLogRepository : IAccessLogRepository
    {
        private readonly LinkGuardiaoDbContext _db;

        public EfAccessLogRepository(LinkGuardiaoDbContext db) => _db = db;

        public async Task RecordAccessAsync(LinkAccess access, CancellationToken ct = default)
        {
            _db.AccessLogs.Add(access);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<LinkAccess>> ListAccessesAsync(string shortCode, int limit, CancellationToken ct = default)
            => await _db.AccessLogs
                .Where(a => a.ShortCode == shortCode)
                .OrderByDescending(a => a.AccessTime)
                .Take(limit)
                .ToListAsync(ct);
    }
}
