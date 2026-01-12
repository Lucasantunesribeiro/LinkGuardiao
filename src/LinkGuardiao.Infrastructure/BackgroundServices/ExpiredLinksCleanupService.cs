using LinkGuardiao.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LinkGuardiao.Infrastructure.BackgroundServices
{
    public class ExpiredLinksCleanupService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExpiredLinksCleanupService> _logger;

        public ExpiredLinksCleanupService(IServiceScopeFactory scopeFactory, ILogger<ExpiredLinksCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CleanupAsync(stoppingToken);
                await Task.Delay(Interval, stoppingToken);
            }
        }

        private async Task CleanupAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var now = DateTime.UtcNow;

                var expiredLinks = await context.ShortenedLinks
                    .Where(link => link.IsActive && link.ExpiresAt != null && link.ExpiresAt <= now)
                    .ToListAsync(stoppingToken);

                if (expiredLinks.Count == 0)
                {
                    return;
                }

                foreach (var link in expiredLinks)
                {
                    link.IsActive = false;
                }

                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Expired links deactivated: {Count}", expiredLinks.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup expired links");
            }
        }
    }
}
