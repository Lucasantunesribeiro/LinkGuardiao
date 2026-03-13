using System.Text.Json;
using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Application.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace LinkGuardiao.Infrastructure.Caching
{
    public sealed class DistributedLinkReadCache : ILinkReadCache
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
        private readonly IDistributedCache _cache;
        private readonly LinkCacheOptions _options;

        public DistributedLinkReadCache(
            IDistributedCache cache,
            IOptions<LinkCacheOptions> options)
        {
            _cache = cache;
            _options = options.Value;
        }

        public async Task<ShortenedLink?> GetAsync(string shortCode, CancellationToken cancellationToken = default)
        {
            var payload = await _cache.GetStringAsync(GetKey(shortCode), cancellationToken);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            var entry = JsonSerializer.Deserialize<LinkCacheEntry>(payload, SerializerOptions);
            return entry?.ToEntity();
        }

        public Task SetAsync(ShortenedLink link, CancellationToken cancellationToken = default)
        {
            var payload = JsonSerializer.Serialize(LinkCacheEntry.FromEntity(link), SerializerOptions);
            return _cache.SetStringAsync(
                GetKey(link.ShortCode),
                payload,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Math.Max(30, _options.HotLinkTtlSeconds))
                },
                cancellationToken);
        }

        public Task RemoveAsync(string shortCode, CancellationToken cancellationToken = default)
        {
            return _cache.RemoveAsync(GetKey(shortCode), cancellationToken);
        }

        private static string GetKey(string shortCode) => $"linkguardiao:links:{shortCode}";

        private sealed class LinkCacheEntry
        {
            public string Id { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public string OriginalUrl { get; set; } = string.Empty;
            public string ShortCode { get; set; } = string.Empty;
            public string? Title { get; set; }
            public string? PasswordHash { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public bool IsActive { get; set; }
            public int ClickCount { get; set; }

            public static LinkCacheEntry FromEntity(ShortenedLink link)
            {
                return new LinkCacheEntry
                {
                    Id = link.Id,
                    UserId = link.UserId,
                    OriginalUrl = link.OriginalUrl,
                    ShortCode = link.ShortCode,
                    Title = link.Title,
                    PasswordHash = link.PasswordHash,
                    CreatedAt = link.CreatedAt,
                    ExpiresAt = link.ExpiresAt,
                    IsActive = link.IsActive,
                    ClickCount = link.ClickCount
                };
            }

            public ShortenedLink ToEntity()
            {
                return new ShortenedLink
                {
                    Id = Id,
                    UserId = UserId,
                    OriginalUrl = OriginalUrl,
                    ShortCode = ShortCode,
                    Title = Title,
                    PasswordHash = PasswordHash,
                    CreatedAt = CreatedAt,
                    ExpiresAt = ExpiresAt,
                    IsActive = IsActive,
                    ClickCount = ClickCount
                };
            }
        }
    }
}
