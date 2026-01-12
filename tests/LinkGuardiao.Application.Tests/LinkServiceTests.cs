using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Services;
using LinkGuardiao.Infrastructure.Data;
using LinkGuardiao.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LinkGuardiao.Application.Tests
{
    public class LinkServiceTests
    {
        [Fact]
        public async Task CreateLinkAsync_HashesPassword()
        {
            var context = CreateDbContext();
            var hasher = new Pbkdf2PasswordHasher();
            var service = new LinkService(context, hasher);

            var link = await service.CreateLinkAsync(new LinkCreateDto
            {
                OriginalUrl = "https://example.com",
                Password = "Secret123"
            }, userId: 1);

            Assert.NotNull(link.PasswordHash);
            Assert.True(hasher.Verify("Secret123", link.PasswordHash!));
        }

        [Fact]
        public async Task GetAllLinksAsync_ReturnsOnlyUserLinks()
        {
            var context = CreateDbContext();
            context.ShortenedLinks.AddRange(
                new LinkGuardiao.Application.Entities.ShortenedLink { UserId = 1, OriginalUrl = "https://a.com", ShortCode = "abc123" },
                new LinkGuardiao.Application.Entities.ShortenedLink { UserId = 2, OriginalUrl = "https://b.com", ShortCode = "def456" });
            await context.SaveChangesAsync();

            var service = new LinkService(context, new Pbkdf2PasswordHasher());
            var links = await service.GetAllLinksAsync(1);

            Assert.Single(links);
        }

        private static ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
