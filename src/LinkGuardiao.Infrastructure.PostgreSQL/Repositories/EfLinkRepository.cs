using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Infrastructure.PostgreSQL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LinkGuardiao.Infrastructure.PostgreSQL.Repositories
{
    public class EfLinkRepository : ILinkRepository
    {
        private readonly LinkGuardiaoDbContext _db;

        public EfLinkRepository(LinkGuardiaoDbContext db) => _db = db;

        public Task<ShortenedLink?> GetByShortCodeAsync(string shortCode, CancellationToken ct = default)
            => _db.Links.FirstOrDefaultAsync(l => l.ShortCode == shortCode, ct);

        public Task<ShortenedLink?> GetByShortCodeForUserAsync(string shortCode, string userId, CancellationToken ct = default)
            => _db.Links.FirstOrDefaultAsync(l => l.ShortCode == shortCode && l.UserId == userId, ct);

        public async Task<IReadOnlyList<ShortenedLink>> ListByUserAsync(string userId, CancellationToken ct = default)
            => await _db.Links.Where(l => l.UserId == userId).OrderByDescending(l => l.CreatedAt).ToListAsync(ct);

        public async Task<bool> TryCreateAsync(ShortenedLink link, CancellationToken ct = default)
        {
            var exists = await _db.Links.AnyAsync(l => l.ShortCode == link.ShortCode, ct);
            if (exists) return false;
            _db.Links.Add(link);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task UpdateAsync(ShortenedLink link, CancellationToken ct = default)
        {
            _db.Links.Update(link);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<bool> DeleteAsync(string shortCode, string userId, CancellationToken ct = default)
        {
            var link = await _db.Links.FirstOrDefaultAsync(l => l.ShortCode == shortCode && l.UserId == userId, ct);
            if (link == null) return false;
            _db.Links.Remove(link);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken ct = default)
            => _db.Links.AnyAsync(l => l.ShortCode == shortCode, ct);

        public async Task IncrementClickCountAsync(string shortCode, CancellationToken ct = default)
        {
            var link = await _db.Links.FirstOrDefaultAsync(l => l.ShortCode == shortCode, ct);
            if (link != null)
            {
                link.ClickCount++;
                await _db.SaveChangesAsync(ct);
            }
        }
    }
}
