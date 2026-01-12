using LinkGuardiao.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LinkGuardiao.Api.Controllers
{
    [ApiController]
    public class RedirectController : ControllerBase
    {
        private readonly ILinkService _linkService;
        private readonly IStatsService _statsService;
        private readonly ILogger<RedirectController> _logger;

        public RedirectController(ILinkService linkService, IStatsService statsService, ILogger<RedirectController> logger)
        {
            _linkService = linkService;
            _statsService = statsService;
            _logger = logger;
        }

        [HttpGet("/r/{shortCode}")]
        [AllowAnonymous]
        [EnableRateLimiting("redirect")]
        public async Task<IActionResult> RedirectToOriginal(string shortCode)
        {
            var link = await _linkService.GetLinkByShortCodeAsync(shortCode);
            if (link == null)
            {
                return NotFound(new { message = "Link não encontrado ou expirado" });
            }

            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var userAgent = Request.Headers.UserAgent.ToString();
                var referrer = Request.Headers.Referer.ToString();

                await _statsService.RecordAccessAsync(shortCode, ipAddress, userAgent, referrer);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record access for {ShortCode}", shortCode);
            }

            return Redirect(link.OriginalUrl);
        }

        [HttpGet("/{shortCode}")]
        [AllowAnonymous]
        [EnableRateLimiting("redirect")]
        public Task<IActionResult> RedirectToOriginalRoot(string shortCode)
        {
            return RedirectToOriginal(shortCode);
        }
    }
}
