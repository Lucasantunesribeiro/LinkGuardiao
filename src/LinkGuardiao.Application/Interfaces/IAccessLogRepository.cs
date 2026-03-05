using LinkGuardiao.Application.Entities;

namespace LinkGuardiao.Application.Interfaces
{
    public interface IAccessLogRepository
    {
        Task RecordAccessAsync(LinkAccess access, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<LinkAccess>> ListAccessesAsync(string shortCode, int limit, CancellationToken cancellationToken = default);
    }
}
