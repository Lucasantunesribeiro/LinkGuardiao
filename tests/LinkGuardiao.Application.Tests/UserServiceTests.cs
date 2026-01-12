using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Application.Services;
using LinkGuardiao.Infrastructure.Data;
using LinkGuardiao.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LinkGuardiao.Application.Tests
{
    public class UserServiceTests
    {
        [Fact]
        public async Task RegisterAsync_CreatesUserAndReturnsToken()
        {
            var context = CreateDbContext();
            var service = new UserService(
                context,
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
            var context = CreateDbContext();
            var service = new UserService(
                context,
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

        private static ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private class FakeJwtTokenService : IJwtTokenService
        {
            public string GenerateToken(User user) => "fake-token";
        }
    }
}
