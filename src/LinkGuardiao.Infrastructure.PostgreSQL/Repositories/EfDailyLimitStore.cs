using System.Globalization;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Infrastructure.PostgreSQL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LinkGuardiao.Infrastructure.PostgreSQL.Repositories
{
    public class EfDailyLimitStore : IDailyLimitStore
    {
        private readonly LinkGuardiaoDbContext _db;

        public EfDailyLimitStore(LinkGuardiaoDbContext db) => _db = db;

        public async Task<bool> TryConsumeAsync(string userId, int limit, CancellationToken cancellationToken = default)
        {
            if (limit <= 0)
            {
                return true;
            }

            var now = DateTime.UtcNow;
            var dayKey = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var limitKey = $"USER#{userId}#DATE#{dayKey}";
            var expiresAtUtc = now.Date.AddDays(2);

            var affectedRows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO daily_limit_counters (limit_key, current_count, expires_at_utc)
                VALUES ({limitKey}, 1, {expiresAtUtc})
                ON CONFLICT (limit_key) DO UPDATE
                SET current_count = daily_limit_counters.current_count + 1,
                    expires_at_utc = EXCLUDED.expires_at_utc
                WHERE daily_limit_counters.current_count < {limit};",
                cancellationToken);

            return affectedRows > 0;
        }
    }
}
