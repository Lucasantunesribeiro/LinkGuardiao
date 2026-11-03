using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Application.Options;
using LinkGuardiao.Application.Security;
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
            var service = new LinkService(
                links,
                new AllowAllDailyLimitStore(),
                hasher,
                new NoOpLinkReadCache(),
                Microsoft.Extensions.Options.Options.Create(new LinkLimitsOptions()));

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

            var service = new LinkService(
                links,
                new AllowAllDailyLimitStore(),
                new Pbkdf2PasswordHasher(),
                new NoOpLinkReadCache(),
                Microsoft.Extensions.Options.Options.Create(new LinkLimitsOptions()));
            var result = await service.GetAllLinksAsync("user-1");

            Assert.Single(result);
        }

        [Fact]
        public async Task CreateLinkAsync_StoresCanonicalAsciiUrl()
        {
            var service = CreateService(new InMemoryLinkRepository());

            var link = await service.CreateLinkAsync(new LinkCreateDto
            {
                OriginalUrl = "  HTTPS://BÜCHER.de:443/caminho com espaço  "
            }, userId: "user-1");

            Assert.Equal("https://xn--bcher-kva.de/caminho%20com%20espa%C3%A7o", link.OriginalUrl);
        }

        [Theory]
        [InlineData("http://169.254.169.254/latest/meta-data/")]
        [InlineData("http://0x7f000001/")]
        [InlineData("javascript:alert(1)")]
        [InlineData("https://admin:pw@example.com/")]
        public async Task CreateLinkAsync_RejectsUnsafeUrlWithoutConsumingDailyLimit(string url)
        {
            var limits = new CountingDailyLimitStore();
            var service = new LinkService(
                new InMemoryLinkRepository(),
                limits,
                new Pbkdf2PasswordHasher(),
                new NoOpLinkReadCache(),
                Microsoft.Extensions.Options.Options.Create(new LinkLimitsOptions { DailyUserCreateLimit = 10 }));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateLinkAsync(new LinkCreateDto { OriginalUrl = url }, "user-1"));

            // O controller mapeia mensagens com "limit" para 429; URL invalida deve virar 400.
            Assert.DoesNotContain("limit", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, limits.Calls);
        }

        [Fact]
        public async Task UpdateLinkAsync_RejectsPrivateDestinationAndKeepsOriginal()
        {
            var links = new InMemoryLinkRepository();
            var service = CreateService(links);
            var link = await service.CreateLinkAsync(new LinkCreateDto { OriginalUrl = "https://example.com/" }, "user-1");

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateLinkAsync(
                link.ShortCode,
                new LinkUpdateDto { OriginalUrl = "http://[::ffff:10.0.0.1]/", IsActive = true },
                "user-1"));

            var stored = await links.GetByShortCodeAsync(link.ShortCode);
            Assert.Equal("https://example.com/", stored!.OriginalUrl);
        }

        [Fact]
        public async Task CreateLinkAsync_NeverGeneratesReservedOrMalformedCodes()
        {
            var service = CreateService(new InMemoryLinkRepository());

            for (var i = 0; i < 200; i++)
            {
                var link = await service.CreateLinkAsync(new LinkCreateDto { OriginalUrl = "https://example.com" }, "user-1");
                Assert.Equal(6, link.ShortCode.Length);
                Assert.True(ShortCodePolicy.IsWellFormed(link.ShortCode));
                Assert.False(ShortCodePolicy.IsReserved(link.ShortCode));
            }
        }

        [Theory]
        [InlineData("health", true)]
        [InlineData("HEALTH", true)]
        [InlineData("Login", true)]
        [InlineData("swagger", true)]
        [InlineData("abc123", false)]
        public void ShortCodePolicy_IsReserved_IsCaseInsensitive(string code, bool expected)
        {
            Assert.Equal(expected, ShortCodePolicy.IsReserved(code));
        }

        [Theory]
        [InlineData("abc123", true)]
        [InlineData("a-b_c", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("../etc", false)]
        [InlineData("abc 12", false)]
        [InlineData("abc%2F", false)]
        [InlineData("ábc123", false)]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", false)]
        public void ShortCodePolicy_IsWellFormed(string? code, bool expected)
        {
            Assert.Equal(expected, ShortCodePolicy.IsWellFormed(code));
        }

        private static LinkService CreateService(ILinkRepository links)
        {
            return new LinkService(
                links,
                new AllowAllDailyLimitStore(),
                new Pbkdf2PasswordHasher(),
                new NoOpLinkReadCache(),
                Microsoft.Extensions.Options.Options.Create(new LinkLimitsOptions()));
        }

        private sealed class CountingDailyLimitStore : IDailyLimitStore
        {
            public int Calls { get; private set; }

            public Task<bool> TryConsumeAsync(string userId, int limit, CancellationToken cancellationToken = default)
            {
                Calls++;
                return Task.FromResult(true);
            }
        }

        private sealed class AllowAllDailyLimitStore : IDailyLimitStore
        {
            public Task<bool> TryConsumeAsync(string userId, int limit, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(true);
            }
        }

        private sealed class NoOpLinkReadCache : ILinkReadCache
        {
            public Task<ShortenedLink?> GetAsync(string shortCode, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<ShortenedLink?>(null);
            }

            public Task SetAsync(ShortenedLink link, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task RemoveAsync(string shortCode, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
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
