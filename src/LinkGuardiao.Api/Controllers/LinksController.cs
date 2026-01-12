using LinkGuardiao.Application.DTOs;
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

            var links = await _linkService.GetAllLinksAsync(userId.Value);
            return Ok(links);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetLink(int id)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var link = await _linkService.GetLinkByIdAsync(id, userId.Value);
            if (link == null)
                return NotFound();

            return Ok(link);
        }

        [HttpGet("{shortCode:length(6)}")]
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

            var createdLink = await _linkService.CreateLinkAsync(linkDto, userId.Value);
            return CreatedAtAction(nameof(GetLink), new { id = createdLink.Id }, createdLink);
        }

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateLink(int id, LinkUpdateDto linkDto)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var link = await _linkService.UpdateLinkAsync(id, linkDto, userId.Value);
            if (link == null)
                return NotFound();

            return Ok(link);
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteLink(int id)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _linkService.DeleteLinkAsync(id, userId.Value);
            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpGet("{id:int}/stats")]
        [Authorize]
        [EnableRateLimiting("stats")]
        public async Task<IActionResult> GetLinkStatsById(int id)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var link = await _linkService.GetLinkByIdAsync(id, userId.Value);
            if (link == null)
                return NotFound();

            try
            {
                var stats = await _statsService.GetLinkStatsAsync(link.ShortCode, userId.Value);
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
                var stats = await _statsService.GetLinkStatsAsync(shortCode, userId.Value);
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
            var isValid = await _linkService.VerifyLinkPasswordAsync(shortCode, password);

            if (!isValid)
                return Unauthorized();

            return Ok();
        }

        private int? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var id) ? id : null;
        }
    }
}
