using LinkGuardiao.API.DTOs;
using LinkGuardiao.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LinkGuardiao.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserService userService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto userDto)
        {
            _logger.LogInformation("Recebida requisição de registro:");
            _logger.LogInformation("Content-Type: {ContentType}", Request.ContentType);
            _logger.LogInformation("Method: {Method}", Request.Method);
            _logger.LogInformation("Path: {Path}", Request.Path);
            _logger.LogInformation("QueryString: {QueryString}", Request.QueryString);
            
            if (Request.Headers.Count > 0) 
            {
                _logger.LogInformation("Headers:");
                foreach (var header in Request.Headers)
                {
                    _logger.LogInformation("  {Key}: {Value}", header.Key, header.Value);
                }
            }
            
            string requestBody = "null";
            try
            {
                if (userDto != null)
                {
                    requestBody = JsonSerializer.Serialize(userDto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao serializar corpo da requisição");
            }
            
            _logger.LogInformation("Corpo da requisição: {RequestBody}", requestBody);
            
            if (userDto == null)
            {
                _logger.LogWarning("Corpo da requisição é nulo");
                return BadRequest(new { message = "O corpo da requisição não pode ser vazio" });
            }
            
            _logger.LogInformation("Valores recebidos: Name={Name}, Email={Email}, Password=***", 
                userDto.Name ?? "null", 
                userDto.Email ?? "null");
            
            if (!ModelState.IsValid)
            {
                var modelErrors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                
                _logger.LogWarning("ModelState inválido: {ModelErrors}", JsonSerializer.Serialize(modelErrors));
                return BadRequest(new { message = "Dados inválidos", errors = modelErrors });
            }

            if (string.IsNullOrEmpty(userDto.Name))
            {
                _logger.LogWarning("Nome não fornecido");
                return BadRequest(new { message = "O nome é obrigatório" });
            }

            if (string.IsNullOrEmpty(userDto.Email))
            {
                _logger.LogWarning("Email não fornecido");
                return BadRequest(new { message = "O email é obrigatório" });
            }

            if (string.IsNullOrEmpty(userDto.Password))
            {
                _logger.LogWarning("Senha não fornecida");
                return BadRequest(new { message = "A senha é obrigatória" });
            }

            var result = await _userService.RegisterAsync(userDto);
            if (!result.Success)
            {
                _logger.LogWarning("Falha no registro: {Message}", result.Message);
                return BadRequest(new { message = result.Message });
            }

            _logger.LogInformation("Registro bem sucedido para o usuário: {Email}", userDto.Email);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto userDto)
        {
            _logger.LogInformation("Recebida requisição de login: {Email}", userDto?.Email);
            
            if (userDto == null)
            {
                _logger.LogWarning("Corpo da requisição é nulo");
                return BadRequest(new { message = "O corpo da requisição não pode ser vazio" });
            }
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Modelo inválido: {ModelErrors}", JsonSerializer.Serialize(ModelState));
                return BadRequest(new { message = "Dados inválidos", errors = ModelState });
            }

            var result = await _userService.LoginAsync(userDto);
            if (!result.Success)
            {
                _logger.LogWarning("Falha no login: {Message}", result.Message);
                return Unauthorized(new { message = result.Message });
            }

            _logger.LogInformation("Login bem sucedido para o usuário: {Email}", userDto.Email);
            return Ok(result);
        }
    }
} 