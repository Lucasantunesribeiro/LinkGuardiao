using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LinkGuardiao.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register(UserRegisterDto userDto)
        {
            var result = await _userService.RegisterAsync(userDto);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login(UserLoginDto userDto)
        {
            var result = await _userService.LoginAsync(userDto);
            if (!result.Success)
            {
                return Unauthorized(new { message = result.Message });
            }

            return Ok(result);
        }
    }
}
