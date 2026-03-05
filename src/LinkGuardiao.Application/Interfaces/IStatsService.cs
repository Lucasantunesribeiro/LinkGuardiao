using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Entities;

namespace LinkGuardiao.Application.Interfaces
{
    public interface IStatsService
    {
        Task<LinkAccess> RecordAccessAsync(string shortCode, string ipAddress, string? userAgent, string? referrer);
        Task<LinkStatsDto> GetLinkStatsAsync(string shortCode, string userId);
    }
}
