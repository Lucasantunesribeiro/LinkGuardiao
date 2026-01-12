using System.Security.Cryptography;
using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LinkGuardiao.Application.Services
{
    public class LinkService : ILinkService
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int ShortCodeLength = 6;

        public LinkService(IApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<IEnumerable<ShortenedLink>> GetAllLinksAsync(int userId)
        {
            return await _context.ShortenedLinks
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .ToListAsync();
        }

        public async Task<ShortenedLink?> GetLinkByIdAsync(int id, int userId)
        {
            return await _context.ShortenedLinks
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
        }

        public async Task<ShortenedLink?> GetLinkByShortCodeAsync(string shortCode)
        {
            var now = DateTime.UtcNow;
            return await _context.ShortenedLinks
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.ShortCode == shortCode
                    && l.IsActive
                    && (l.ExpiresAt == null || l.ExpiresAt > now));
        }

        public async Task<ShortenedLink> CreateLinkAsync(LinkCreateDto linkDto, int userId)
        {
            var shortCode = await GenerateUniqueShortCodeAsync();

            var link = new ShortenedLink
            {
                OriginalUrl = linkDto.OriginalUrl,
                ShortCode = shortCode,
                Title = linkDto.Title,
                UserId = userId,
                ExpiresAt = linkDto.ExpiresAt
            };

            if (!string.IsNullOrWhiteSpace(linkDto.Password))
            {
                link.PasswordHash = _passwordHasher.Hash(linkDto.Password);
            }

            _context.ShortenedLinks.Add(link);
            await _context.SaveChangesAsync();

            return link;
        }

        public async Task<ShortenedLink?> UpdateLinkAsync(int id, LinkUpdateDto linkDto, int userId)
        {
            var link = await _context.ShortenedLinks
                .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);

            if (link == null)
                return null;

            link.OriginalUrl = linkDto.OriginalUrl;
            link.Title = linkDto.Title;
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

            await _context.SaveChangesAsync();

            return link;
        }

        public async Task<bool> DeleteLinkAsync(int id, int userId)
        {
            var link = await _context.ShortenedLinks
                .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);

            if (link == null)
                return false;

            _context.ShortenedLinks.Remove(link);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> VerifyLinkPasswordAsync(string shortCode, string password)
        {
            var link = await _context.ShortenedLinks
                .FirstOrDefaultAsync(l => l.ShortCode == shortCode);

            if (link == null || string.IsNullOrEmpty(link.PasswordHash))
                return false;

            return _passwordHasher.Verify(password, link.PasswordHash);
        }

        public async Task<string> GenerateUniqueShortCodeAsync()
        {
            string shortCode;

            do
            {
                shortCode = GenerateRandomShortCode();
            } while (await _context.ShortenedLinks.AnyAsync(l => l.ShortCode == shortCode));

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
