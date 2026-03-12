using LinkGuardiao.Application.Entities;

namespace LinkGuardiao.Application.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
        Task CreateAsync(RefreshToken token, CancellationToken ct = default);
        Task RevokeAsync(string tokenHash, CancellationToken ct = default);
        Task RevokeAllForUserAsync(string userId, CancellationToken ct = default);
    }
}
