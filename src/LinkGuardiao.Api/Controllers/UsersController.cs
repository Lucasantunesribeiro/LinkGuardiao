using System.Security.Claims;
using LinkGuardiao.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkGuardiao.Api.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var parsedId))
                return Unauthorized();

            var user = await _userService.GetByIdAsync(parsedId);
            if (user == null)
                return NotFound();

            return Ok(new { id = user.Id, username = user.Username, email = user.Email });
        }
    }
}
