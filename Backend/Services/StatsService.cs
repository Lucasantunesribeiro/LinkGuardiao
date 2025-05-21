using LinkGuardiao.API.Data;
using LinkGuardiao.API.DTOs;
using LinkGuardiao.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkGuardiao.API.Services
{
    public class StatsService : IStatsService
    {
        private readonly ApplicationDbContext _context;

        public StatsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<LinkAccess> RecordAccessAsync(string shortCode, string ipAddress, string? userAgent, string? referrer)
        {
            var link = await _context.ShortenedLinks
                .FirstOrDefaultAsync(l => l.ShortCode == shortCode);

            if (link == null)
                throw new InvalidOperationException("Link não encontrado");

            var access = new LinkAccess
            {
                ShortenedLinkId = link.Id,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                ReferrerUrl = referrer,
                AccessTime = DateTime.UtcNow
            };

            // Aqui você pode adicionar lógica para extrair informações do User-Agent
            if (!string.IsNullOrEmpty(userAgent))
            {
                // Implementação simplificada - em produção use uma biblioteca de User Agent parsing
                access.Browser = userAgent.Contains("Firefox") ? "Firefox" : 
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

            _context.LinkAccesses.Add(access);
            await _context.SaveChangesAsync();

            return access;
        }

        public async Task<LinkStatsDto> GetLinkStatsAsync(string shortCode)
        {
            var link = await _context.ShortenedLinks
                .Include(l => l.Accesses)
                .FirstOrDefaultAsync(l => l.ShortCode == shortCode);

            if (link == null)
                throw new InvalidOperationException("Link não encontrado");

            var totalClicks = await GetTotalClicksAsync(link.Id);
            var browserStats = await GetBrowserStatsAsync(link.Id);
            var ipStats = await GetIpStatsAsync(link.Id);
            var clicksByDate = await GetClicksByDateAsync(link.Id);

            return new LinkStatsDto
            {
                LinkId = link.Id,
                ShortCode = link.ShortCode,
                OriginalUrl = link.OriginalUrl,
                TotalClicks = totalClicks,
                BrowserStats = browserStats.ToList(),
                TopIpAddresses = ipStats.ToList(),
                ClicksByDate = clicksByDate.ToList()
            };
        }

        public async Task<int> GetTotalClicksAsync(int linkId)
        {
            return await _context.LinkAccesses
                .CountAsync(a => a.ShortenedLinkId == linkId);
        }

        public async Task<IEnumerable<BrowserStatsDto>> GetBrowserStatsAsync(int linkId)
        {
            var totalClicks = await GetTotalClicksAsync(linkId);
            if (totalClicks == 0) return Enumerable.Empty<BrowserStatsDto>();

            var browserStats = await _context.LinkAccesses
                .Where(a => a.ShortenedLinkId == linkId && a.Browser != null)
                .GroupBy(a => a.Browser)
                .Select(g => new BrowserStatsDto
                {
                    Browser = g.Key ?? "Desconhecido",
                    Count = g.Count(),
                    Percentage = (double)g.Count() / totalClicks * 100
                })
                .ToListAsync();

            return browserStats;
        }

        public async Task<IEnumerable<IpStatsDto>> GetIpStatsAsync(int linkId)
        {
            var totalClicks = await GetTotalClicksAsync(linkId);
            if (totalClicks == 0) return Enumerable.Empty<IpStatsDto>();

            var ipStats = await _context.LinkAccesses
                .Where(a => a.ShortenedLinkId == linkId)
                .GroupBy(a => a.IpAddress)
                .Select(g => new IpStatsDto
                {
                    IpAddress = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / totalClicks * 100
                })
                .OrderByDescending(s => s.Count)
                .Take(10)
                .ToListAsync();

            return ipStats;
        }

        public async Task<IEnumerable<DateStatsDto>> GetClicksByDateAsync(int linkId)
        {
            var clicksByDate = await _context.LinkAccesses
                .Where(a => a.ShortenedLinkId == linkId)
                .GroupBy(a => a.AccessTime.Date)
                .Select(g => new DateStatsDto
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(s => s.Date)
                .ToListAsync();

            return clicksByDate;
        }
    }
} 