using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LinkGuardiao.Api.Tests
{
    public class LinkFlowTests : IClassFixture<ApiTestFactory>
    {
        private readonly HttpClient _client;
        private readonly ApiTestFactory _factory;

        public LinkFlowTests(ApiTestFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task CreateLinkAndFetchStats()
        {
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
            {
                name = "Test User",
                email = "test@example.com",
                password = "Secret123"
            });

            var registerContent = await registerResponse.Content.ReadAsStringAsync();
            Assert.True(registerResponse.IsSuccessStatusCode, registerContent);
            var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

            Assert.NotNull(auth);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

            var createResponse = await _client.PostAsJsonAsync("/api/links", new
            {
                originalUrl = "https://example.com",
                title = "Example"
            });

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var link = await createResponse.Content.ReadFromJsonAsync<LinkResponse>();
            Assert.NotNull(link);

            var redirectResponse = await _client.GetAsync($"/{link!.ShortCode}");
            Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);

            var statsResponse = await _client.GetAsync($"/api/links/{link.Id}/stats");
            statsResponse.EnsureSuccessStatusCode();

            var stats = await statsResponse.Content.ReadFromJsonAsync<LinkStatsResponse>();
            Assert.NotNull(stats);
            Assert.True(stats!.TotalClicks >= 1);
        }

        [Fact]
        public async Task PublicLink_NoPasswordNeeded_Returns302()
        {
            var auth = await RegisterAndLoginAsync("public-link@example.com");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

            var createResponse = await _client.PostAsJsonAsync("/api/links", new
            {
                originalUrl = "https://example.com",
                title = "Public"
            });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var link = await createResponse.Content.ReadFromJsonAsync<LinkResponse>();

            _client.DefaultRequestHeaders.Authorization = null;
            var redirectResponse = await _client.GetAsync($"/{link!.ShortCode}");
            Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);
        }

        [Fact]
        public async Task PublicShortCodeLookup_DoesNotExposeOriginalUrl()
        {
            var auth = await RegisterAndLoginAsync("public-lookup@example.com");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

            var createResponse = await _client.PostAsJsonAsync("/api/links", new
            {
                originalUrl = "https://example.com/private-destination",
                title = "Lookup Test",
                password = "secret1234"
            });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var link = await createResponse.Content.ReadFromJsonAsync<LinkResponse>();

            _client.DefaultRequestHeaders.Authorization = null;
            var lookupResponse = await _client.GetAsync($"/api/links/code/{link!.ShortCode}");
            lookupResponse.EnsureSuccessStatusCode();

            var payload = await lookupResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(payload.GetProperty("isPasswordProtected").GetBoolean());
            Assert.False(payload.TryGetProperty("originalUrl", out _));
        }

        [Fact]
        public async Task PasswordProtectedLink_NoHeader_Returns401WithRequiresPasswordFlag()
        {
            var auth = await RegisterAndLoginAsync("pw-noheader@example.com");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

            var createResponse = await _client.PostAsJsonAsync("/api/links", new
            {
                originalUrl = "https://example.com",
                title = "Protected",
                password = "secret1234"
            });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var link = await createResponse.Content.ReadFromJsonAsync<LinkResponse>();

            _client.DefaultRequestHeaders.Authorization = null;
            var redirectResponse = await _client.GetAsync($"/{link!.ShortCode}");
            Assert.Equal(HttpStatusCode.Unauthorized, redirectResponse.StatusCode);

            var body = await redirectResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("requiresPassword").GetBoolean());
        }

        [Fact]
        public async Task PasswordProtectedLink_WrongPassword_Returns401WithInvalidFlag()
        {
            var auth = await RegisterAndLoginAsync("pw-wrong@example.com");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

            var createResponse = await _client.PostAsJsonAsync("/api/links", new
            {
                originalUrl = "https://example.com",
                title = "Protected",
                password = "correct1234"
            });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var link = await createResponse.Content.ReadFromJsonAsync<LinkResponse>();

            _client.DefaultRequestHeaders.Authorization = null;
            var request = new HttpRequestMessage(HttpMethod.Get, $"/{link!.ShortCode}");
            request.Headers.Add("X-Link-Password", "wrong-password");
            var redirectResponse = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, redirectResponse.StatusCode);

            var body = await redirectResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("requiresPassword").GetBoolean());
            Assert.True(body.GetProperty("invalidPassword").GetBoolean());
        }

        [Fact]
        public async Task PasswordProtectedLink_CorrectPassword_Returns302()
        {
            var auth = await RegisterAndLoginAsync("pw-correct@example.com");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

            var createResponse = await _client.PostAsJsonAsync("/api/links", new
            {
                originalUrl = "https://example.com",
                title = "Protected",
                password = "rightpass1234"
            });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var link = await createResponse.Content.ReadFromJsonAsync<LinkResponse>();

            _client.DefaultRequestHeaders.Authorization = null;
            var request = new HttpRequestMessage(HttpMethod.Get, $"/{link!.ShortCode}");
            request.Headers.Add("X-Link-Password", "rightpass1234");
            var redirectResponse = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);
        }

        [Fact]
        public async Task PasswordProtectedLink_AccessGrant_RedirectsThroughOfficialEndpoint()
        {
            var auth = await RegisterAndLoginAsync("pw-grant@example.com");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

            var createResponse = await _client.PostAsJsonAsync("/api/links", new
            {
                originalUrl = "https://example.com",
                title = "Protected",
                password = "grant-pass-1234"
            });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var link = await createResponse.Content.ReadFromJsonAsync<LinkResponse>();

            _client.DefaultRequestHeaders.Authorization = null;

            var grantResponse = await _client.PostAsJsonAsync($"/api/links/access-grant/{link!.ShortCode}", new
            {
                password = "grant-pass-1234"
            });
            grantResponse.EnsureSuccessStatusCode();

            var grantPayload = await grantResponse.Content.ReadFromJsonAsync<JsonElement>();
            var accessGrant = grantPayload.GetProperty("accessGrant").GetString();
            Assert.False(string.IsNullOrWhiteSpace(accessGrant));

            var queue = _factory.Services.GetRequiredService<InMemoryAnalyticsQueue>();
            var before = queue.Messages.Count;

            var redirectResponse = await _client.GetAsync($"/{link.ShortCode}?grant={Uri.EscapeDataString(accessGrant!)}");
            Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);
            Assert.Equal(before + 1, queue.Messages.Count);
        }

        [Fact]
        public async Task Redirect_EnqueuesAccessMessageAndReturns302()
        {
            var auth = await RegisterAndLoginAsync("redirect-queue@example.com");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

            var createResponse = await _client.PostAsJsonAsync("/api/links", new
            {
                originalUrl = "https://example.com",
                title = "Queue Test"
            });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var link = await createResponse.Content.ReadFromJsonAsync<LinkResponse>();

            _client.DefaultRequestHeaders.Authorization = null;

            var queue = _factory.Services.GetRequiredService<InMemoryAnalyticsQueue>();
            var before = queue.Messages.Count;

            var redirectResponse = await _client.GetAsync($"/{link!.ShortCode}");
            Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);
            Assert.Equal(before + 1, queue.Messages.Count);
            Assert.Equal(link.ShortCode, queue.Messages.Last().ShortCode);
        }

        [Fact]
        public async Task RefreshToken_ValidToken_ReturnsNewPair()
        {
            var auth = await RegisterAndLoginAsync("refresh-valid@example.com");
            Assert.NotNull(auth.RefreshToken);

            var response = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = auth.RefreshToken });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var newAuth = await response.Content.ReadFromJsonAsync<AuthResponse>();
            Assert.NotNull(newAuth);
            Assert.NotNull(newAuth!.Token);
            Assert.NotNull(newAuth.RefreshToken);
            // Refresh token must rotate (new random token each time)
            Assert.NotEqual(auth.RefreshToken, newAuth.RefreshToken);
        }

        [Fact]
        public async Task RefreshToken_ExpiredToken_Returns401()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = "not-a-valid-token" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_RevokedToken_Returns401()
        {
            var auth = await RegisterAndLoginAsync("refresh-revoked@example.com");

            // Use the refresh token once to rotate it
            var firstRefresh = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = auth.RefreshToken });
            Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);

            // Try using the OLD refresh token again — should be revoked now
            var secondRefresh = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = auth.RefreshToken });
            Assert.Equal(HttpStatusCode.Unauthorized, secondRefresh.StatusCode);
        }

        [Fact]
        public async Task Logout_RevokesToken_SubsequentRefreshFails()
        {
            var auth = await RegisterAndLoginAsync("logout-test@example.com");

            var logoutResponse = await _client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = auth.RefreshToken });
            Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

            var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = auth.RefreshToken });
            Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
        }

        private async Task<AuthResponse> RegisterAndLoginAsync(string email)
        {
            var response = await _client.PostAsJsonAsync("/api/auth/register", new
            {
                name = "Test User",
                email,
                password = "Secret123!"
            });
            var content = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, content);
            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
            Assert.NotNull(auth);
            return auth!;
        }

        private sealed record AuthResponse(string Token, string? RefreshToken);
        private sealed record LinkResponse(string Id, string ShortCode);
        private sealed record LinkStatsResponse(int TotalClicks);
    }
}
