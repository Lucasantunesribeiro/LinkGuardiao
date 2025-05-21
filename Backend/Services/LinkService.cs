using LinkGuardiao.API.Data;
using LinkGuardiao.API.DTOs;
using LinkGuardiao.API.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace LinkGuardiao.API.Services
{
    public class LinkService : ILinkService
    {
        private readonly ApplicationDbContext _context;
        private static readonly Random _random = new();
        private const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int ShortCodeLength = 6;

        public LinkService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ShortenedLink>> GetAllLinksAsync()
        {
            return await _context.ShortenedLinks
                .Include(l => l.User)
                .ToListAsync();
        }

        public async Task<ShortenedLink?> GetLinkByIdAsync(int id)
        {
            return await _context.ShortenedLinks
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<ShortenedLink?> GetLinkByShortCodeAsync(string shortCode)
        {
            return await _context.ShortenedLinks
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.ShortCode == shortCode && l.IsActive);
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
            
            if (!string.IsNullOrEmpty(linkDto.Password))
            {
                link.PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(linkDto.Password);
            }
            
            _context.ShortenedLinks.Add(link);
            await _context.SaveChangesAsync();
            
            return link;
        }

        public async Task<ShortenedLink?> UpdateLinkAsync(int id, LinkUpdateDto linkDto)
        {
            var link = await _context.ShortenedLinks.FindAsync(id);
            
            if (link == null)
                return null;
                
            link.OriginalUrl = linkDto.OriginalUrl;
            link.Title = linkDto.Title;
            link.IsActive = linkDto.IsActive;
            link.ExpiresAt = linkDto.ExpiresAt;
            
            if (!string.IsNullOrEmpty(linkDto.Password))
            {
                link.PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(linkDto.Password);
            }
            else if (linkDto.RemovePassword)
            {
                link.PasswordHash = null;
            }
            
            await _context.SaveChangesAsync();
            
            return link;
        }

        public async Task<bool> DeleteLinkAsync(int id)
        {
            var link = await _context.ShortenedLinks.FindAsync(id);
            
            if (link == null)
                return false;
                
            _context.ShortenedLinks.Remove(link);
            await _context.SaveChangesAsync();
            
            return true;
        }

        public async Task<bool> VerifyLinkPasswordAsync(string shortCode, string password)
        {
            var link = await _context.ShortenedLinks.FirstOrDefaultAsync(l => l.ShortCode == shortCode);
            
            if (link == null || string.IsNullOrEmpty(link.PasswordHash))
                return false;
                
            return BCrypt.Net.BCrypt.EnhancedVerify(password, link.PasswordHash);
        }

        public async Task<string> GenerateUniqueShortCodeAsync()
        {
            string shortCode;
            bool exists;
            
            do
            {
                shortCode = GenerateRandomShortCode();
                exists = await _context.ShortenedLinks.AnyAsync(l => l.ShortCode == shortCode);
            } while (exists);
            
            return shortCode;
        }

        private static string GenerateRandomShortCode()
        {
            var chars = new char[ShortCodeLength];
            
            for (int i = 0; i < ShortCodeLength; i++)
            {
                chars[i] = AllowedChars[_random.Next(AllowedChars.Length)];
            }
            
            return new string(chars);
        }
    }
}