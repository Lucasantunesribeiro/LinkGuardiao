using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Entities;

namespace LinkGuardiao.Application.Interfaces
{
    public interface ILinkService
    {
        Task<IEnumerable<ShortenedLink>> GetAllLinksAsync(int userId);
        Task<ShortenedLink?> GetLinkByIdAsync(int id, int userId);
        Task<ShortenedLink?> GetLinkByShortCodeAsync(string shortCode);
        Task<ShortenedLink> CreateLinkAsync(LinkCreateDto linkDto, int userId);
        Task<ShortenedLink?> UpdateLinkAsync(int id, LinkUpdateDto linkDto, int userId);
        Task<bool> DeleteLinkAsync(int id, int userId);
        Task<bool> VerifyLinkPasswordAsync(string shortCode, string password);
        Task<string> GenerateUniqueShortCodeAsync();
    }
}
