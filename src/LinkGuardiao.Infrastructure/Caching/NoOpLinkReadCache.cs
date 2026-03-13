using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;

namespace LinkGuardiao.Infrastructure.Caching
{
    public sealed class NoOpLinkReadCache : ILinkReadCache
    {
        public Task<ShortenedLink?> GetAsync(string shortCode, CancellationToken cancellationToken = default)
            => Task.FromResult<ShortenedLink?>(null);

        public Task SetAsync(ShortenedLink link, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveAsync(string shortCode, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
