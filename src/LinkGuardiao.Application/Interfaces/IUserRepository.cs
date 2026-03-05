using LinkGuardiao.Application.Entities;

namespace LinkGuardiao.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
        Task CreateAsync(User user, CancellationToken cancellationToken = default);
    }
}
