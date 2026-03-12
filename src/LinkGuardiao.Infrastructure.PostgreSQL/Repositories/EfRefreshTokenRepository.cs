using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Infrastructure.PostgreSQL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LinkGuardiao.Infrastructure.PostgreSQL.Repositories
{
    public class EfRefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly LinkGuardiaoDbContext _db;

        public EfRefreshTokenRepository(LinkGuardiaoDbContext db) => _db = db;

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
            => _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == tokenHash, ct);

        public async Task CreateAsync(RefreshToken token, CancellationToken ct = default)
        {
            _db.RefreshTokens.Add(token);
            await _db.SaveChangesAsync(ct);
        }

        public async Task RevokeAsync(string tokenHash, CancellationToken ct = default)
        {
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == tokenHash, ct);
            if (token != null)
            {
                token.IsRevoked = true;
                await _db.SaveChangesAsync(ct);
            }
        }

        public async Task RevokeAllForUserAsync(string userId, CancellationToken ct = default)
        {
            var tokens = await _db.RefreshTokens.Where(r => r.UserId == userId).ToListAsync(ct);
            foreach (var token in tokens)
                token.IsRevoked = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
