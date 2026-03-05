using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Entities;

namespace LinkGuardiao.Application.Interfaces
{
    public interface ILinkService
    {
        Task<IEnumerable<ShortenedLink>> GetAllLinksAsync(string userId);
        Task<ShortenedLink?> GetLinkByIdAsync(string id, string userId);
        Task<ShortenedLink?> GetLinkByShortCodeAsync(string shortCode);
        Task<ShortenedLink> CreateLinkAsync(LinkCreateDto linkDto, string userId);
        Task<ShortenedLink?> UpdateLinkAsync(string id, LinkUpdateDto linkDto, string userId);
        Task<bool> DeleteLinkAsync(string id, string userId);
        Task<bool> VerifyLinkPasswordAsync(string shortCode, string password);
        Task<string> GenerateUniqueShortCodeAsync();
    }
}
