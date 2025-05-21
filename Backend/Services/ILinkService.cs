using LinkGuardiao.API.DTOs;
using LinkGuardiao.API.Models;

namespace LinkGuardiao.API.Services
{
    public interface ILinkService
    {
        Task<IEnumerable<ShortenedLink>> GetAllLinksAsync();
        Task<ShortenedLink?> GetLinkByIdAsync(int id);
        Task<ShortenedLink?> GetLinkByShortCodeAsync(string shortCode);
        Task<ShortenedLink> CreateLinkAsync(LinkCreateDto linkDto, int userId);
        Task<ShortenedLink?> UpdateLinkAsync(int id, LinkUpdateDto linkDto);
        Task<bool> DeleteLinkAsync(int id);
        Task<bool> VerifyLinkPasswordAsync(string shortCode, string password);
        Task<string> GenerateUniqueShortCodeAsync();
    }
}