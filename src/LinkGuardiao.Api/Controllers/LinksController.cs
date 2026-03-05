using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace LinkGuardiao.Api.Controllers
{
    [Route("api/links")]
    [ApiController]
    public class LinksController : ControllerBase
    {
        private readonly ILinkService _linkService;
        private readonly IStatsService _statsService;

        public LinksController(ILinkService linkService, IStatsService statsService)
        {
            _linkService = linkService;
            _statsService = statsService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllLinks()
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var links = await _linkService.GetAllLinksAsync(userId);
            return Ok(links);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetLink(string id)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var link = await _linkService.GetLinkByIdAsync(id, userId);
            if (link == null)
                return NotFound();

            return Ok(link);
        }

        [HttpGet("code/{shortCode:length(6)}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLinkByShortCode(string shortCode)
        {
            var link = await _linkService.GetLinkByShortCodeAsync(shortCode);
            if (link == null)
                return NotFound();

            return Ok(link);
        }
        [HttpPost]
        [Authorize]
        [EnableRateLimiting("link-create")]
        public async Task<IActionResult> CreateLink(LinkCreateDto linkDto)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                var createdLink = await _linkService.CreateLinkAsync(linkDto, userId);
                return CreatedAtAction(nameof(GetLink), new { id = createdLink.Id }, createdLink);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("limit", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateLink(string id, LinkUpdateDto linkDto)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            ShortenedLink? link;
            try
            {
                link = await _linkService.UpdateLinkAsync(id, linkDto, userId);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            if (link == null)
                return NotFound();

            return Ok(link);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteLink(string id)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _linkService.DeleteLinkAsync(id, userId);
            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpGet("{id}/stats")]
        [Authorize]
        [EnableRateLimiting("stats")]
        public async Task<IActionResult> GetLinkStatsById(string id)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var link = await _linkService.GetLinkByIdAsync(id, userId);
            if (link == null)
                return NotFound();

            try
            {
                var stats = await _statsService.GetLinkStatsAsync(link.ShortCode, userId);
                return Ok(stats);
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }
        [HttpGet("stats/{shortCode}")]
        [Authorize]
        [EnableRateLimiting("stats")]
        public async Task<IActionResult> GetLinkStats(string shortCode)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                var stats = await _statsService.GetLinkStatsAsync(shortCode, userId);
                return Ok(stats);
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }

        [HttpPost("verify-password/{shortCode}")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> VerifyPassword(string shortCode, [FromBody] string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return BadRequest(new { message = "Password is required." });
            }

            var isValid = await _linkService.VerifyLinkPasswordAsync(shortCode, password);

            if (!isValid)
                return Unauthorized();

            return Ok();
        }

        private string? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrWhiteSpace(userIdClaim) ? null : userIdClaim;
        }
    }
}
