using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Application.Options;
using LinkGuardiao.Application.Services;
using LinkGuardiao.Infrastructure.Security;
using Xunit;

namespace LinkGuardiao.Application.Tests
{
    public class LinkServiceTests
    {
        [Fact]
        public async Task CreateLinkAsync_HashesPassword()
        {
            var links = new InMemoryLinkRepository();
            var hasher = new Pbkdf2PasswordHasher();
            var service = new LinkService(links, new AllowAllDailyLimitStore(), hasher, Microsoft.Extensions.Options.Options.Create(new LinkLimitsOptions()));

            var link = await service.CreateLinkAsync(new LinkCreateDto
            {
                OriginalUrl = "https://example.com",
                Password = "Secret123"
            }, userId: "user-1");

            Assert.NotNull(link.PasswordHash);
            Assert.True(hasher.Verify("Secret123", link.PasswordHash!));
        }

        [Fact]
        public async Task GetAllLinksAsync_ReturnsOnlyUserLinks()
        {
            var links = new InMemoryLinkRepository();
            await links.TryCreateAsync(new ShortenedLink { UserId = "user-1", OriginalUrl = "https://a.com", ShortCode = "abc123", Id = "abc123", CreatedAt = DateTime.UtcNow });
            await links.TryCreateAsync(new ShortenedLink { UserId = "user-2", OriginalUrl = "https://b.com", ShortCode = "def456", Id = "def456", CreatedAt = DateTime.UtcNow });

            var service = new LinkService(links, new AllowAllDailyLimitStore(), new Pbkdf2PasswordHasher(), Microsoft.Extensions.Options.Options.Create(new LinkLimitsOptions()));
            var result = await service.GetAllLinksAsync("user-1");

            Assert.Single(result);
        }

        private sealed class AllowAllDailyLimitStore : IDailyLimitStore
        {
            public Task<bool> TryConsumeAsync(string userId, int limit, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(true);
            }
        }

        private sealed class InMemoryLinkRepository : ILinkRepository
        {
            private readonly Dictionary<string, ShortenedLink> _links = new(StringComparer.OrdinalIgnoreCase);

            public Task<ShortenedLink?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
            {
                _links.TryGetValue(shortCode, out var link);
                return Task.FromResult(link);
            }

            public Task<ShortenedLink?> GetByShortCodeForUserAsync(string shortCode, string userId, CancellationToken cancellationToken = default)
            {
                _links.TryGetValue(shortCode, out var link);
                if (link == null || link.UserId != userId)
                {
                    return Task.FromResult<ShortenedLink?>(null);
                }

                return Task.FromResult<ShortenedLink?>(link);
            }

            public Task<IReadOnlyList<ShortenedLink>> ListByUserAsync(string userId, CancellationToken cancellationToken = default)
            {
                var items = _links.Values.Where(link => link.UserId == userId).ToList();
                return Task.FromResult<IReadOnlyList<ShortenedLink>>(items);
            }

            public Task<bool> TryCreateAsync(ShortenedLink link, CancellationToken cancellationToken = default)
            {
                if (_links.ContainsKey(link.ShortCode))
                {
                    return Task.FromResult(false);
                }

                _links[link.ShortCode] = link;
                return Task.FromResult(true);
            }

            public Task UpdateAsync(ShortenedLink link, CancellationToken cancellationToken = default)
            {
                _links[link.ShortCode] = link;
                return Task.CompletedTask;
            }

            public Task<bool> DeleteAsync(string shortCode, string userId, CancellationToken cancellationToken = default)
            {
                if (_links.TryGetValue(shortCode, out var link) && link.UserId == userId)
                {
                    _links.Remove(shortCode);
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }

            public Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_links.ContainsKey(shortCode));
            }

            public Task IncrementClickCountAsync(string shortCode, CancellationToken cancellationToken = default)
            {
                if (_links.TryGetValue(shortCode, out var link))
                {
                    link.ClickCount += 1;
                }

                return Task.CompletedTask;
            }
        }
    }
}
