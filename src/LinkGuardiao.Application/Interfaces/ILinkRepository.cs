using LinkGuardiao.Application.Entities;

namespace LinkGuardiao.Application.Interfaces
{
    public interface ILinkRepository
    {
        Task<ShortenedLink?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);
        Task<ShortenedLink?> GetByShortCodeForUserAsync(string shortCode, string userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ShortenedLink>> ListByUserAsync(string userId, CancellationToken cancellationToken = default);
        Task<bool> TryCreateAsync(ShortenedLink link, CancellationToken cancellationToken = default);
        Task UpdateAsync(ShortenedLink link, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string shortCode, string userId, CancellationToken cancellationToken = default);
        Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default);
        Task IncrementClickCountAsync(string shortCode, CancellationToken cancellationToken = default);
    }
}
