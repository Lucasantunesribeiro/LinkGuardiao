namespace LinkGuardiao.Application.Interfaces
{
    public interface IDailyLimitStore
    {
        Task<bool> TryConsumeAsync(string userId, int limit, CancellationToken cancellationToken = default);
    }
}
