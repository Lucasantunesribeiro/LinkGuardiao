using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;

namespace LinkGuardiao.Infrastructure.Messaging
{
    public class NoOpAnalyticsQueue : IAnalyticsQueue
    {
        public Task EnqueueAsync(AccessLogMessage message, CancellationToken ct = default) => Task.CompletedTask;
    }
}
