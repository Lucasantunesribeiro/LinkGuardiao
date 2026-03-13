using LinkGuardiao.Application.Entities;

namespace LinkGuardiao.Application.Interfaces
{
    public interface ILinkReadCache
    {
        Task<ShortenedLink?> GetAsync(string shortCode, CancellationToken cancellationToken = default);
        Task SetAsync(ShortenedLink link, CancellationToken cancellationToken = default);
        Task RemoveAsync(string shortCode, CancellationToken cancellationToken = default);
    }
}
