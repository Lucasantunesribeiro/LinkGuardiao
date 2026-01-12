using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Entities;

namespace LinkGuardiao.Application.Interfaces
{
    public interface IStatsService
    {
        Task<LinkAccess> RecordAccessAsync(string shortCode, string ipAddress, string? userAgent, string? referrer);
        Task<LinkStatsDto> GetLinkStatsAsync(string shortCode, int userId);
        Task<int> GetTotalClicksAsync(int linkId);
        Task<IEnumerable<BrowserStatsDto>> GetBrowserStatsAsync(int linkId);
        Task<IEnumerable<IpStatsDto>> GetIpStatsAsync(int linkId);
        Task<IEnumerable<DateStatsDto>> GetClicksByDateAsync(int linkId);
    }
}
