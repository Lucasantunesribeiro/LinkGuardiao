using LinkGuardiao.Application.Entities;
using LinkGuardiao.Infrastructure.Messaging;
using Xunit;

namespace LinkGuardiao.Api.Tests
{
    public class DirectAnalyticsQueueTests
    {
        [Fact]
        public async Task EnqueueAsync_RecordsAccessAndIncrementsClickCountOncePerEvent()
        {
            var links = new InMemoryLinkRepository();
            var accessLogs = new InMemoryAccessLogRepository();
            await links.TryCreateAsync(new ShortenedLink
            {
                Id = "abc123",
                ShortCode = "abc123",
                UserId = "user-1",
                OriginalUrl = "https://example.com/",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });

            var queue = new DirectAnalyticsQueue(accessLogs, links);
            var message = new AccessLogMessage
            {
                Id = "event-1",
                ShortCode = "abc123",
                IpAddress = "203.0.113.10",
                Browser = "Firefox",
                AccessTime = DateTime.UtcNow
            };

            await queue.EnqueueAsync(message);
            await queue.EnqueueAsync(message); // reentrega do mesmo evento nao conta de novo

            var accesses = await accessLogs.ListAccessesAsync("abc123", 10);
            var access = Assert.Single(accesses);
            Assert.Equal("Firefox", access.Browser);
            Assert.Equal(1, (await links.GetByShortCodeAsync("abc123"))!.ClickCount);
        }
    }
}
