using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Exceptions;
using LinkGuardiao.Application.Security;
using LinkGuardiao.Infrastructure.PostgreSQL.Repositories;
using Xunit;

namespace LinkGuardiao.Infrastructure.PostgreSQL.Tests
{
    public class PostgreSqlIntegrationTests : IClassFixture<PostgreSqlIntegrationFixture>
    {
        private readonly PostgreSqlIntegrationFixture _fixture;

        public PostgreSqlIntegrationTests(PostgreSqlIntegrationFixture fixture)
        {
            _fixture = fixture;
        }

        [SkippableFact]
        public async Task EfUserRepository_CreateAsync_ThrowsUserExistsExceptionForDuplicateEmail()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetDatabaseAsync();

            await using var db = _fixture.CreateDbContext();
            var repository = new EfUserRepository(db);

            await repository.CreateAsync(new User
            {
                Id = Guid.NewGuid().ToString("N"),
                Email = "duplicate@example.com",
                Username = "first-user",
                PasswordHash = "hash-1",
                CreatedAt = DateTime.UtcNow
            });

            await Assert.ThrowsAsync<UserExistsException>(() =>
                repository.CreateAsync(new User
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Email = "duplicate@example.com",
                    Username = "second-user",
                    PasswordHash = "hash-2",
                    CreatedAt = DateTime.UtcNow
                }));
        }

        [SkippableFact]
        public async Task EfDailyLimitStore_TryConsumeAsync_StopsAtConfiguredLimit()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetDatabaseAsync();

            await using var db = _fixture.CreateDbContext();
            var repository = new EfDailyLimitStore(db);

            Assert.True(await repository.TryConsumeAsync("user-123", 2));
            Assert.True(await repository.TryConsumeAsync("user-123", 2));
            Assert.False(await repository.TryConsumeAsync("user-123", 2));
        }

        [SkippableFact]
        public async Task EfRefreshTokenRepository_RevokeAllForUserAsync_UpdatesOnlyTargetUser()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetDatabaseAsync();

            await using var db = _fixture.CreateDbContext();
            var repository = new EfRefreshTokenRepository(db);

            await repository.CreateAsync(new RefreshToken
            {
                TokenHash = "hash-1",
                UserId = "user-1",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
            await repository.CreateAsync(new RefreshToken
            {
                TokenHash = "hash-2",
                UserId = "user-1",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
            await repository.CreateAsync(new RefreshToken
            {
                TokenHash = "hash-3",
                UserId = "user-2",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            await repository.RevokeAllForUserAsync("user-1");

            Assert.True((await repository.GetByTokenHashAsync("hash-1"))!.IsRevoked);
            Assert.True((await repository.GetByTokenHashAsync("hash-2"))!.IsRevoked);
            Assert.False((await repository.GetByTokenHashAsync("hash-3"))!.IsRevoked);
        }
    
        [SkippableFact]
        public async Task EfLinkRepository_TryCreateAsync_PersistsUrlAtValidationLimit()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetDatabaseAsync();

            var prefix = "https://example.com/";
            var url = prefix + new string('a', UrlSafety.MaxUrlLength - prefix.Length);
            Assert.True(UrlSafety.TryNormalize(url, out var normalized, out _));

            await using (var db = _fixture.CreateDbContext())
            {
                var repository = new EfLinkRepository(db);
                Assert.True(await repository.TryCreateAsync(new ShortenedLink
                {
                    Id = "maxlen",
                    ShortCode = "maxlen",
                    UserId = "user-1",
                    OriginalUrl = normalized,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                }));
            }

            await using var readDb = _fixture.CreateDbContext();
            var stored = await new EfLinkRepository(readDb).GetByShortCodeAsync("maxlen");
            Assert.Equal(UrlSafety.MaxUrlLength, stored!.OriginalUrl.Length);
        }
    }
}
