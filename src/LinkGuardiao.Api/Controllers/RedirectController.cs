using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LinkGuardiao.Api.Controllers
{
    [ApiController]
    public class RedirectController : ControllerBase
    {
        private readonly ILinkService _linkService;
        private readonly IAnalyticsQueue _analyticsQueue;
        private readonly ILinkAccessGrantService _linkAccessGrantService;
        private readonly ILogger<RedirectController> _logger;

        public RedirectController(
            ILinkService linkService,
            IAnalyticsQueue analyticsQueue,
            ILinkAccessGrantService linkAccessGrantService,
            ILogger<RedirectController> logger)
        {
            _linkService = linkService;
            _analyticsQueue = analyticsQueue;
            _linkAccessGrantService = linkAccessGrantService;
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

            if (link.IsPasswordProtected)
            {
                var accessGrant = Request.Query["grant"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(accessGrant)
                    && _linkAccessGrantService.TryValidate(shortCode, accessGrant))
                {
                    return await RedirectWithAnalyticsAsync(shortCode, link);
                }

                var pwd = Request.Headers["X-Link-Password"].FirstOrDefault();
                if (string.IsNullOrEmpty(pwd))
                    return StatusCode(StatusCodes.Status401Unauthorized, new { requiresPassword = true, shortCode });
                var valid = await _linkService.VerifyLinkPasswordAsync(shortCode, pwd);
                if (!valid)
                    return StatusCode(StatusCodes.Status401Unauthorized, new { requiresPassword = true, invalidPassword = true });
            }

            return await RedirectWithAnalyticsAsync(shortCode, link);
        }

        private async Task<IActionResult> RedirectWithAnalyticsAsync(string shortCode, ShortenedLink link)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var userAgent = Request.Headers.UserAgent.ToString();
                var referrer = Request.Headers.Referer.ToString();

                var message = new AccessLogMessage
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ShortCode = shortCode,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    ReferrerUrl = referrer,
                    AccessTime = DateTime.UtcNow
                };

                if (!string.IsNullOrEmpty(userAgent))
                {
                    message.Browser = userAgent.Contains("Firefox") ? "Firefox" :
                        userAgent.Contains("Edg/") ? "Edge" :
                        userAgent.Contains("Chrome") ? "Chrome" :
                        userAgent.Contains("Safari") ? "Safari" : "Outro";

                    message.OperatingSystem = userAgent.Contains("Windows") ? "Windows" :
                        userAgent.Contains("Mac") ? "MacOS" :
                        userAgent.Contains("Linux") ? "Linux" : "Outro";

                    message.DeviceType = userAgent.Contains("Mobile") ? "Mobile" :
                        userAgent.Contains("Tablet") ? "Tablet" : "Desktop";
                }

                await _analyticsQueue.EnqueueAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enqueue analytics for {ShortCode}", shortCode);
            }

            if (!UrlSafety.IsSafeHttpUrl(link.OriginalUrl, out var safeUri))
            {
                _logger.LogWarning("Blocked unsafe redirect for {ShortCode}", shortCode);
                return StatusCode(StatusCodes.Status410Gone, new { message = "Link invalido" });
            }

            return Redirect(safeUri!.ToString());
        }

        [HttpGet("/{shortCode:length(6)}")]
        [AllowAnonymous]
        [EnableRateLimiting("redirect")]
        public Task<IActionResult> RedirectToOriginalRoot(string shortCode)
        {
            return RedirectToOriginal(shortCode);
        }
    }
}
