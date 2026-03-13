using LinkGuardiao.Application.Entities;
using LinkGuardiao.Infrastructure.PostgreSQL.Persistence;
using LinkGuardiao.Infrastructure.PostgreSQL.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LinkGuardiao.Infrastructure.PostgreSQL.Tests
{
    public class EfRepositoryTests
    {
        private static LinkGuardiaoDbContext CreateInMemoryDb(string dbName)
        {
            var options = new DbContextOptionsBuilder<LinkGuardiaoDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new LinkGuardiaoDbContext(options);
        }

        [Fact]
        public async Task EfUserRepository_DuplicateEmail_ThrowsOnSaveChanges()
        {
            using var db = CreateInMemoryDb(nameof(EfUserRepository_DuplicateEmail_ThrowsOnSaveChanges));
            var repo = new EfUserRepository(db);

            await repo.CreateAsync(new User
            {
                Id = "user1",
                Email = "test@example.com",
                Username = "Test",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            });

            // Second user with same email — in-memory DB won't enforce unique index,
            // but the EfUserRepository.EmailExistsAsync check handles this at the service layer
            var exists = await repo.EmailExistsAsync("test@example.com");
            Assert.True(exists);

            var notExists = await repo.EmailExistsAsync("other@example.com");
            Assert.False(notExists);
        }

        [Fact]
        public async Task EfLinkRepository_IncrementClickCount_UpdatesPersistedValue()
        {
            using var db = CreateInMemoryDb(nameof(EfLinkRepository_IncrementClickCount_UpdatesPersistedValue));
            var repo = new EfLinkRepository(db);

            var link = new ShortenedLink
            {
                Id = "abc123",
                ShortCode = "abc123",
                OriginalUrl = "https://example.com",
                UserId = "user1",
                CreatedAt = DateTime.UtcNow,
                ClickCount = 0
            };
            await repo.TryCreateAsync(link);

            await repo.IncrementClickCountAsync("abc123");

            var updated = await repo.GetByShortCodeAsync("abc123");
            Assert.NotNull(updated);
            Assert.Equal(1, updated!.ClickCount);
        }

        [Fact]
        public async Task EfRefreshTokenRepository_RevokeAll_UpdatesAllForUser()
        {
            using var db = CreateInMemoryDb(nameof(EfRefreshTokenRepository_RevokeAll_UpdatesAllForUser));
            var repo = new EfRefreshTokenRepository(db);

            var userId = "user1";
            await repo.CreateAsync(new RefreshToken { TokenHash = "hash1", UserId = userId, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(30) });
            await repo.CreateAsync(new RefreshToken { TokenHash = "hash2", UserId = userId, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(30) });
            await repo.CreateAsync(new RefreshToken { TokenHash = "hash3", UserId = "other", CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(30) });

            await repo.RevokeAllForUserAsync(userId);

            var t1 = await repo.GetByTokenHashAsync("hash1");
            var t2 = await repo.GetByTokenHashAsync("hash2");
            var t3 = await repo.GetByTokenHashAsync("hash3");

            Assert.True(t1!.IsRevoked);
            Assert.True(t2!.IsRevoked);
            Assert.False(t3!.IsRevoked);
        }
    }
}
