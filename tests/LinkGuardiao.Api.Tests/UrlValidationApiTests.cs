using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LinkGuardiao.Api.Tests
{
    public class UrlValidationApiTests : IClassFixture<ApiTestFactory>
    {
        private readonly ApiTestFactory _factory;

        public UrlValidationApiTests(ApiTestFactory factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("data:text/html,<script>alert(1)</script>")]
        [InlineData("file:///etc/passwd")]
        [InlineData("ftp://example.com/")]
        [InlineData("gopher://127.0.0.1:6379/_FLUSHALL")]
        [InlineData("https://user:pass@example.com/")]
        [InlineData("http://localhost:8080/")]
        [InlineData("http://169.254.169.254/latest/meta-data/")]
        [InlineData("http://2130706433/")]
        [InlineData("http://0x7f000001/")]
        [InlineData("http://127.1/")]
        [InlineData("http://[::ffff:127.0.0.1]/")]
        [InlineData("http://10.0.0.5/")]
        [InlineData("http://metadata.google.internal/")]
        public async Task CreateLink_RejectsUnsafeDestination(string url)
        {
            var client = await CreateAuthenticatedClientAsync();

            var response = await client.PostAsJsonAsync("/api/links", new { originalUrl = url });

            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"{url} => {(int)response.StatusCode} {body}");
            Assert.Contains("originalUrl", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateLink_ReturnsSpecificReasonForTheField()
        {
            var client = await CreateAuthenticatedClientAsync();

            var response = await client.PostAsJsonAsync("/api/links", new { originalUrl = "http://169.254.169.254/" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            using var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var messages = json.RootElement.GetProperty("errors").GetProperty("OriginalUrl")
                .EnumerateArray().Select(m => m.GetString()).ToList();
            // Uma unica mensagem, especifica do motivo (o [Url] generico em ingles foi removido do DTO).
            Assert.Equal(new[] { "O endereço aponta para um IP privado, de loopback ou reservado." }, messages);
        }

        [Fact]
        public async Task CreateLink_RejectsUrlLongerThanLimit()
        {
            var client = await CreateAuthenticatedClientAsync();

            var response = await client.PostAsJsonAsync("/api/links", new
            {
                originalUrl = "https://example.com/" + new string('a', 2100)
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateLink_RejectsPrivateDestination()
        {
            var client = await CreateAuthenticatedClientAsync();
            var create = await client.PostAsJsonAsync("/api/links", new { originalUrl = "https://example.com" });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var link = await create.Content.ReadFromJsonAsync<LinkResponse>();

            var update = await client.PutAsJsonAsync($"/api/links/{link!.Id}", new
            {
                originalUrl = "http://192.168.0.1/admin",
                isActive = true
            });

            Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        }

        [Fact]
        public async Task CreateLink_StoresIdnAsPunycodeAndRedirectsWithAsciiLocation()
        {
            var client = await CreateAuthenticatedClientAsync();

            var create = await client.PostAsJsonAsync("/api/links", new { originalUrl = "https://bücher.de/ä?q=é" });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var link = await create.Content.ReadFromJsonAsync<LinkResponse>();
            Assert.Equal("https://xn--bcher-kva.de/%C3%A4?q=%C3%A9", link!.OriginalUrl);

            var anonymous = CreateClient();
            var redirect = await anonymous.GetAsync($"/r/{link.ShortCode}");

            Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);
            Assert.Equal("https://xn--bcher-kva.de/%C3%A4?q=%C3%A9", redirect.Headers.Location!.OriginalString);
        }

        [Theory]
        [InlineData("http://127.0.0.1:6379/")]
        [InlineData("http://169.254.169.254/latest/meta-data/iam/")]
        [InlineData("javascript:alert(1)")]
        [InlineData("https://example.com/\r\nSet-Cookie: x=y")]
        public async Task Redirect_LegacyStoredUnsafeUrl_Returns410WithoutEnqueuingAnalytics(string storedUrl)
        {
            var shortCode = "Lg" + Guid.NewGuid().ToString("N")[..4];
            var links = _factory.Services.GetRequiredService<ILinkRepository>();
            await links.TryCreateAsync(new ShortenedLink
            {
                Id = shortCode,
                ShortCode = shortCode,
                OriginalUrl = storedUrl,
                UserId = "legacy-user",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });

            var queue = _factory.Services.GetRequiredService<InMemoryAnalyticsQueue>();
            var before = queue.Messages.Count(m => m.ShortCode == shortCode);

            var response = await CreateClient().GetAsync($"/{shortCode}");

            Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
            Assert.Null(response.Headers.Location);
            Assert.Equal(before, queue.Messages.Count(m => m.ShortCode == shortCode));
        }

        [Theory]
        [InlineData("/r/abc%2E%2E")]
        [InlineData("/r/abc$123")]
        [InlineData("/r/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("/api/links/code/ab%20c12")]
        public async Task PublicShortCodeRoutes_RejectMalformedCodes(string path)
        {
            var response = await CreateClient().GetAsync(path);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AccessGrant_RejectsMalformedCode()
        {
            var response = await CreateClient().PostAsJsonAsync("/api/links/access-grant/bad$code", new { password = "x" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private HttpClient CreateClient()
        {
            return _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }

        private async Task<HttpClient> CreateAuthenticatedClientAsync()
        {
            var client = CreateClient();
            var email = $"url-{Guid.NewGuid():N}@example.com";
            var response = await client.PostAsJsonAsync("/api/auth/register", new
            {
                name = "Url Tester",
                email,
                password = "Secret123!"
            });
            var content = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, content);
            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
            return client;
        }

        private sealed record AuthResponse(string Token);
        private sealed record LinkResponse(string Id, string ShortCode, string OriginalUrl);
    }
}
