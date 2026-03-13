namespace LinkGuardiao.Infrastructure.PostgreSQL.Persistence
{
    public class DailyLimitCounter
    {
        public string LimitKey { get; set; } = string.Empty;
        public int CurrentCount { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }
}
