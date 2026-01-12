using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LinkGuardiao.Api.Tests
{
    public class LinkFlowTests : IClassFixture<ApiTestFactory>
    {
        private readonly HttpClient _client;

        public LinkFlowTests(ApiTestFactory factory)
        {
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

        private sealed record AuthResponse(string Token);
        private sealed record LinkResponse(int Id, string ShortCode);
        private sealed record LinkStatsResponse(int TotalClicks);
    }
}
