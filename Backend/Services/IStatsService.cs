using LinkGuardiao.API.DTOs;
using LinkGuardiao.API.Models;

namespace LinkGuardiao.API.Services
{
    public interface IStatsService
    {
        Task<LinkAccess> RecordAccessAsync(string shortCode, string ipAddress, string? userAgent, string? referrer);
        Task<LinkStatsDto> GetLinkStatsAsync(string shortCode);
        Task<int> GetTotalClicksAsync(int linkId);
        Task<IEnumerable<BrowserStatsDto>> GetBrowserStatsAsync(int linkId);
        Task<IEnumerable<IpStatsDto>> GetIpStatsAsync(int linkId);
        Task<IEnumerable<DateStatsDto>> GetClicksByDateAsync(int linkId);
    }
}