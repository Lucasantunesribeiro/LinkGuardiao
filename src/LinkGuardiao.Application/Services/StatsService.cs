using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;

namespace LinkGuardiao.Application.Services
{
    public class StatsService : IStatsService
    {
        private const int MaxAccessItems = 1000;
        private readonly ILinkRepository _links;
        private readonly IAccessLogRepository _accessLogs;

        public StatsService(ILinkRepository links, IAccessLogRepository accessLogs)
        {
            _links = links;
            _accessLogs = accessLogs;
        }

        public async Task<LinkAccess> RecordAccessAsync(string shortCode, string ipAddress, string? userAgent, string? referrer)
        {
            var now = DateTime.UtcNow;
            var link = await _links.GetByShortCodeAsync(shortCode);
            if (link == null || !link.IsActive || (link.ExpiresAt.HasValue && link.ExpiresAt.Value <= now))
            {
                throw new InvalidOperationException("Link não encontrado");
            }

            var access = new LinkAccess
            {
                Id = Guid.NewGuid().ToString("N"),
                ShortCode = shortCode,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                ReferrerUrl = referrer,
                AccessTime = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(userAgent))
            {
                access.Browser = userAgent.Contains("Firefox") ? "Firefox" :
                    userAgent.Contains("Edg/") ? "Edge" :
                    userAgent.Contains("Chrome") ? "Chrome" :
                    userAgent.Contains("Safari") ? "Safari" :
                    "Outro";

                access.OperatingSystem = userAgent.Contains("Windows") ? "Windows" :
                    userAgent.Contains("Mac") ? "MacOS" :
                    userAgent.Contains("Linux") ? "Linux" :
                    "Outro";

                access.DeviceType = userAgent.Contains("Mobile") ? "Mobile" :
                    userAgent.Contains("Tablet") ? "Tablet" :
                    "Desktop";
            }

            await _accessLogs.RecordAccessAsync(access);
            await _links.IncrementClickCountAsync(shortCode);
            return access;
        }

        public async Task<LinkStatsDto> GetLinkStatsAsync(string shortCode, string userId)
        {
            var link = await _links.GetByShortCodeForUserAsync(shortCode, userId);
            if (link == null)
            {
                throw new InvalidOperationException("Link não encontrado");
            }

            var accessLogs = await _accessLogs.ListAccessesAsync(shortCode, MaxAccessItems);
            var totalClicks = link.ClickCount;
            var denominator = accessLogs.Count > 0 ? accessLogs.Count : 1;

            var browserStats = accessLogs
                .Where(a => !string.IsNullOrWhiteSpace(a.Browser))
                .GroupBy(a => a.Browser ?? "Desconhecido")
                .Select(g => new BrowserStatsDto
                {
                    Browser = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / denominator * 100
                })
                .ToList();

            var ipStats = accessLogs
                .GroupBy(a => a.IpAddress)
                .Select(g => new IpStatsDto
                {
                    IpAddress = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / denominator * 100
                })
                .OrderByDescending(s => s.Count)
                .Take(10)
                .ToList();

            var clicksByDate = accessLogs
                .GroupBy(a => a.AccessTime.Date)
                .Select(g => new DateStatsDto
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(s => s.Date)
                .ToList();

            return new LinkStatsDto
            {
                LinkId = link.Id,
                ShortCode = link.ShortCode,
                OriginalUrl = link.OriginalUrl,
                TotalClicks = totalClicks,
                BrowserStats = browserStats,
                TopIpAddresses = ipStats,
                ClicksByDate = clicksByDate
            };
        }
    }
}
