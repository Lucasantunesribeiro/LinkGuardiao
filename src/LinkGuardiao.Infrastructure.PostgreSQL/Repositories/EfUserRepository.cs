using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Infrastructure.PostgreSQL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LinkGuardiao.Infrastructure.PostgreSQL.Repositories
{
    public class EfUserRepository : IUserRepository
    {
        private readonly LinkGuardiaoDbContext _db;

        public EfUserRepository(LinkGuardiaoDbContext db) => _db = db;

        public Task<User?> GetByIdAsync(string userId, CancellationToken ct = default)
            => _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
            => _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
            => _db.Users.AnyAsync(u => u.Email == email, ct);

        public async Task CreateAsync(User user, CancellationToken ct = default)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
        }
    }
}
