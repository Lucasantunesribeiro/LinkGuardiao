using System.Security.Cryptography;
using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Application.Options;
using LinkGuardiao.Application.Security;
using Microsoft.Extensions.Options;

namespace LinkGuardiao.Application.Services
{
    public class LinkService : ILinkService
    {
        private readonly ILinkRepository _links;
        private readonly IDailyLimitStore _dailyLimitStore;
        private readonly IPasswordHasher _passwordHasher;
        private readonly LinkLimitsOptions _limits;
        private const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int ShortCodeLength = 6;

        public LinkService(
            ILinkRepository links,
            IDailyLimitStore dailyLimitStore,
            IPasswordHasher passwordHasher,
            IOptions<LinkLimitsOptions> options)
        {
            _links = links;
            _dailyLimitStore = dailyLimitStore;
            _passwordHasher = passwordHasher;
            _limits = options.Value;
        }

        public async Task<IEnumerable<ShortenedLink>> GetAllLinksAsync(string userId)
        {
            return await _links.ListByUserAsync(userId);
        }

        public Task<ShortenedLink?> GetLinkByIdAsync(string id, string userId)
        {
            return _links.GetByShortCodeForUserAsync(id, userId);
        }

        public async Task<ShortenedLink?> GetLinkByShortCodeAsync(string shortCode)
        {
            var link = await _links.GetByShortCodeAsync(shortCode);
            if (link == null)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            if (!link.IsActive)
            {
                return null;
            }

            if (link.ExpiresAt.HasValue && link.ExpiresAt.Value <= now)
            {
                return null;
            }

            return link;
        }

        public async Task<ShortenedLink> CreateLinkAsync(LinkCreateDto linkDto, string userId)
        {
            if (!UrlSafety.IsSafeHttpUrl(linkDto.OriginalUrl, out var safeUri))
            {
                throw new InvalidOperationException("Invalid URL.");
            }

            if (_limits.DailyUserCreateLimit > 0)
            {
                var allowed = await _dailyLimitStore.TryConsumeAsync(userId, _limits.DailyUserCreateLimit);
                if (!allowed)
                {
                    throw new InvalidOperationException("Daily link creation limit reached.");
                }
            }

            var normalizedUrl = safeUri!.ToString();
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var shortCode = GenerateRandomShortCode();
                var link = new ShortenedLink
                {
                    Id = shortCode,
                    ShortCode = shortCode,
                    OriginalUrl = normalizedUrl,
                    Title = linkDto.Title?.Trim(),
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = linkDto.ExpiresAt,
                    IsActive = true
                };

                if (!string.IsNullOrWhiteSpace(linkDto.Password))
                {
                    link.PasswordHash = _passwordHasher.Hash(linkDto.Password);
                }

                if (await _links.TryCreateAsync(link))
                {
                    return link;
                }
            }

            throw new InvalidOperationException("Unable to generate a unique short code.");
        }

        public async Task<ShortenedLink?> UpdateLinkAsync(string id, LinkUpdateDto linkDto, string userId)
        {
            var link = await _links.GetByShortCodeForUserAsync(id, userId);
            if (link == null)
            {
                return null;
            }

            if (!UrlSafety.IsSafeHttpUrl(linkDto.OriginalUrl, out var safeUri))
            {
                throw new InvalidOperationException("Invalid URL.");
            }

            link.OriginalUrl = safeUri!.ToString();
            link.Title = linkDto.Title?.Trim();
            link.IsActive = linkDto.IsActive;
            link.ExpiresAt = linkDto.ExpiresAt;
            if (!string.IsNullOrWhiteSpace(linkDto.Password))
            {
                link.PasswordHash = _passwordHasher.Hash(linkDto.Password);
            }
            else if (linkDto.RemovePassword)
            {
                link.PasswordHash = null;
            }

            await _links.UpdateAsync(link);
            return link;
        }

        public Task<bool> DeleteLinkAsync(string id, string userId)
        {
            return _links.DeleteAsync(id, userId);
        }

        public async Task<bool> VerifyLinkPasswordAsync(string shortCode, string password)
        {
            var link = await _links.GetByShortCodeAsync(shortCode);
            if (link == null || string.IsNullOrEmpty(link.PasswordHash))
            {
                return false;
            }

            return _passwordHasher.Verify(password, link.PasswordHash);
        }

        public async Task<string> GenerateUniqueShortCodeAsync()
        {
            string shortCode;

            do
            {
                shortCode = GenerateRandomShortCode();
            } while (await _links.ShortCodeExistsAsync(shortCode));

            return shortCode;
        }

        private static string GenerateRandomShortCode()
        {
            Span<byte> buffer = stackalloc byte[ShortCodeLength];
            RandomNumberGenerator.Fill(buffer);
            var chars = new char[ShortCodeLength];

            for (int i = 0; i < ShortCodeLength; i++)
            {
                chars[i] = AllowedChars[buffer[i] % AllowedChars.Length];
            }

            return new string(chars);
        }
    }
}
