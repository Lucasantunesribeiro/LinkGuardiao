using LinkGuardiao.API.DTOs;
using LinkGuardiao.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using LinkGuardiao.API.Data;

namespace LinkGuardiao.API.Controllers
{
    [Route("api/links")]
    [ApiController]
    public class LinksController : ControllerBase
    {
        private readonly ILinkService _linkService;
        private readonly IStatsService _statsService;
        private readonly ILogger<LinksController> _logger;
        private readonly ApplicationDbContext _db;

        public LinksController(ILinkService linkService, IStatsService statsService, ILogger<LinksController> logger, ApplicationDbContext db)
        {
            _linkService = linkService;
            _statsService = statsService;
            _logger = logger;
            _db = db;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllLinks()
        {
            _logger.LogInformation("Buscando todos os links do usuário");

            // Logar informações sobre a requisição
            _logger.LogInformation("Headers da requisição:");
            foreach (var header in Request.Headers)
            {
                _logger.LogInformation($"  {header.Key}: {header.Value}");
            }

            // Verificar token e claims
            _logger.LogInformation("Verificando token e claims do usuário");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation($"ID do usuário extraído do token: {userId ?? "null"}");

            try
            {
                var links = await _linkService.GetAllLinksAsync();
                _logger.LogInformation("Encontrados {Count} links", links.Count());
                return Ok(links);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar links");
                return StatusCode(500, "Erro interno ao buscar links");
            }
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetLink(int id)
        {
            var link = await _linkService.GetLinkByIdAsync(id);
            if (link == null)
                return NotFound();
            return Ok(link);
        }

        [HttpGet("{shortCode:length(6)}")]
        public async Task<IActionResult> GetLinkByShortCode(string shortCode)
        {
            // Se o shortCode for só dígitos, trate como id para não conflitar
            if (int.TryParse(shortCode, out _))
                return NotFound();

            var link = await _linkService.GetLinkByShortCodeAsync(shortCode);
            if (link == null)
                return NotFound();
            return Ok(link);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateLink(LinkCreateDto linkDto)
        {
            _logger.LogInformation("Recebida requisição para criar link: {LinkData}", new
            {
                linkDto.OriginalUrl,
                linkDto.Title,
                HasPassword = !string.IsNullOrEmpty(linkDto.Password),
                ExpiresAt = linkDto.ExpiresAt
            });

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Modelo inválido ao criar link. Erros: {Errors}",
                    string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            _logger.LogInformation("Criando link para o usuário: {UserId}", userId);

            try
            {
                var createdLink = await _linkService.CreateLinkAsync(linkDto, userId);
                _logger.LogInformation("Link criado com sucesso: {LinkId}", createdLink.Id);
                return CreatedAtAction(nameof(GetLink), new { id = createdLink.Id }, createdLink);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar link");
                return StatusCode(500, "Erro interno ao criar link");
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateLink(int id, LinkUpdateDto linkDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var link = await _linkService.UpdateLinkAsync(id, linkDto);

            if (link == null)
                return NotFound();

            return Ok(link);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteLink(int id)
        {
            var result = await _linkService.DeleteLinkAsync(id);

            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpGet("stats/{shortCode}")]
        [Authorize]
        public async Task<IActionResult> GetLinkStats(string shortCode)
        {
            var stats = await _statsService.GetLinkStatsAsync(shortCode);
            return Ok(stats);
        }

        [HttpPost("verify-password/{shortCode}")]
        public async Task<IActionResult> VerifyPassword(string shortCode, [FromBody] string password)
        {
            var isValid = await _linkService.VerifyLinkPasswordAsync(shortCode, password);

            if (!isValid)
                return Unauthorized();

            return Ok();
        }

        [HttpGet("/r/{shortCode}")]
        public IActionResult RedirectToOriginal(string shortCode)
        {
            var link = _db.ShortenedLinks
                .FirstOrDefault(l => l.ShortCode == shortCode && l.IsActive && (l.ExpiresAt == null || l.ExpiresAt > DateTime.UtcNow));
            if (link == null)
                return NotFound(new { message = "Link não encontrado ou expirado" });

            link.ClickCount++;
            _db.SaveChanges();

            return Redirect(link.OriginalUrl);
        }
    }

    [ApiController]
    public class RedirectController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public RedirectController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("/{shortCode}")]
        public IActionResult RedirectToOriginal(string shortCode)
        {
            var link = _db.ShortenedLinks
                .FirstOrDefault(l => l.ShortCode == shortCode && l.IsActive && (l.ExpiresAt == null || l.ExpiresAt > DateTime.UtcNow));
            if (link == null)
                return NotFound(new { message = "Link não encontrado ou expirado" });

            link.ClickCount++;
            _db.SaveChanges();

            return Redirect(link.OriginalUrl);
        }
    }
}