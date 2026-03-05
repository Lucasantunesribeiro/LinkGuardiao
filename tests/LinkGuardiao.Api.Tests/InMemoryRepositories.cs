using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;

namespace LinkGuardiao.Api.Tests
{
    public sealed class InMemoryLinkRepository : ILinkRepository
    {
        private readonly Dictionary<string, ShortenedLink> _links = new(StringComparer.OrdinalIgnoreCase);

        public Task<ShortenedLink?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
        {
            _links.TryGetValue(shortCode, out var link);
            return Task.FromResult(link);
        }

        public Task<ShortenedLink?> GetByShortCodeForUserAsync(string shortCode, string userId, CancellationToken cancellationToken = default)
        {
            _links.TryGetValue(shortCode, out var link);
            if (link == null || link.UserId != userId)
            {
                return Task.FromResult<ShortenedLink?>(null);
            }

            return Task.FromResult<ShortenedLink?>(link);
        }

        public Task<IReadOnlyList<ShortenedLink>> ListByUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            var items = _links.Values.Where(link => link.UserId == userId).ToList();
            return Task.FromResult<IReadOnlyList<ShortenedLink>>(items);
        }

        public Task<bool> TryCreateAsync(ShortenedLink link, CancellationToken cancellationToken = default)
        {
            if (_links.ContainsKey(link.ShortCode))
            {
                return Task.FromResult(false);
            }

            _links[link.ShortCode] = link;
            return Task.FromResult(true);
        }

        public Task UpdateAsync(ShortenedLink link, CancellationToken cancellationToken = default)
        {
            _links[link.ShortCode] = link;
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(string shortCode, string userId, CancellationToken cancellationToken = default)
        {
            if (_links.TryGetValue(shortCode, out var link) && link.UserId == userId)
            {
                _links.Remove(shortCode);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_links.ContainsKey(shortCode));
        }

        public Task IncrementClickCountAsync(string shortCode, CancellationToken cancellationToken = default)
        {
            if (_links.TryGetValue(shortCode, out var link))
            {
                link.ClickCount += 1;
            }

            return Task.CompletedTask;
        }
    }

    public sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly Dictionary<string, User> _usersById = new();
        private readonly Dictionary<string, string> _userIdByEmail = new(StringComparer.OrdinalIgnoreCase);

        public Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            _usersById.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (_userIdByEmail.TryGetValue(email, out var userId) && _usersById.TryGetValue(userId, out var user))
            {
                return Task.FromResult<User?>(user);
            }

            return Task.FromResult<User?>(null);
        }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_userIdByEmail.ContainsKey(email));
        }

        public Task CreateAsync(User user, CancellationToken cancellationToken = default)
        {
            _usersById[user.Id] = user;
            _userIdByEmail[user.Email] = user.Id;
            return Task.CompletedTask;
        }
    }

    public sealed class InMemoryAccessLogRepository : IAccessLogRepository
    {
        private readonly List<LinkAccess> _accesses = new();

        public Task RecordAccessAsync(LinkAccess access, CancellationToken cancellationToken = default)
        {
            _accesses.Add(access);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<LinkAccess>> ListAccessesAsync(string shortCode, int limit, CancellationToken cancellationToken = default)
        {
            var items = _accesses
                .Where(access => access.ShortCode == shortCode)
                .OrderByDescending(access => access.AccessTime)
                .Take(limit)
                .ToList();
            return Task.FromResult<IReadOnlyList<LinkAccess>>(items);
        }
    }

    public sealed class AllowAllDailyLimitStore : IDailyLimitStore
    {
        public Task<bool> TryConsumeAsync(string userId, int limit, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
