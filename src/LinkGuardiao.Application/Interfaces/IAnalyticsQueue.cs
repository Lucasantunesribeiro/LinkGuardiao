using LinkGuardiao.Application.Entities;

namespace LinkGuardiao.Application.Interfaces
{
    public interface IAnalyticsQueue
    {
        Task EnqueueAsync(AccessLogMessage message, CancellationToken ct = default);
    }
}
