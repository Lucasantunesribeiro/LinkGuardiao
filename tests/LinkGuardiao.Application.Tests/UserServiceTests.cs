using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Application.Services;
using LinkGuardiao.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LinkGuardiao.Application.Tests
{
    public class UserServiceTests
    {
        [Fact]
        public async Task RegisterAsync_CreatesUserAndReturnsToken()
        {
            var users = new InMemoryUserRepository();
            var service = new UserService(
                users,
                new Pbkdf2PasswordHasher(),
                new FakeJwtTokenService(),
                NullLogger<UserService>.Instance);

            var result = await service.RegisterAsync(new UserRegisterDto
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = "Secret123"
            });

            Assert.True(result.Success);
            Assert.Equal("fake-token", result.Token);
            Assert.NotNull(result.User);
            Assert.Equal("test@example.com", result.User!.Email);
        }

        [Fact]
        public async Task LoginAsync_ReturnsFailureForInvalidPassword()
        {
            var users = new InMemoryUserRepository();
            var service = new UserService(
                users,
                new Pbkdf2PasswordHasher(),
                new FakeJwtTokenService(),
                NullLogger<UserService>.Instance);

            await service.RegisterAsync(new UserRegisterDto
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = "Secret123"
            });

            var result = await service.LoginAsync(new UserLoginDto
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            });

            Assert.False(result.Success);
            Assert.Null(result.Token);
        }

        private sealed class FakeJwtTokenService : IJwtTokenService
        {
            public string GenerateToken(User user) => "fake-token";
        }

        private sealed class InMemoryUserRepository : IUserRepository
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
    }
}
