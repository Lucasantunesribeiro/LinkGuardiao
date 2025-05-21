using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LinkGuardiao.API.Data;
using LinkGuardiao.API.DTOs;
using LinkGuardiao.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LinkGuardiao.API.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context, IConfiguration configuration, ILogger<UserService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResult> RegisterAsync(UserRegisterDto userDto)
        {
            try
            {
                _logger.LogInformation("Iniciando registro para usuário: {Email}", userDto.Email);
                
                if (userDto == null)
                {
                    _logger.LogWarning("UserDto é nulo");
                    return new AuthResult { Success = false, Message = "Dados de usuário inválidos" };
                }
                
                if (string.IsNullOrEmpty(userDto.Name))
                {
                    _logger.LogWarning("Nome do usuário é nulo ou vazio");
                    return new AuthResult { Success = false, Message = "O nome é obrigatório" };
                }
                
                if (string.IsNullOrEmpty(userDto.Email))
                {
                    _logger.LogWarning("Email do usuário é nulo ou vazio");
                    return new AuthResult { Success = false, Message = "O e-mail é obrigatório" };
                }
                
                if (string.IsNullOrEmpty(userDto.Password))
                {
                    _logger.LogWarning("Senha do usuário é nula ou vazia");
                    return new AuthResult { Success = false, Message = "A senha é obrigatória" };
                }

                var userExists = await _context.Users.AnyAsync(u => u.Email == userDto.Email);
                if (userExists)
                {
                    _logger.LogWarning("Usuário com e-mail {Email} já existe", userDto.Email);
                    return new AuthResult { Success = false, Message = "E-mail já cadastrado" };
                }

                var user = new User
                {
                    Username = userDto.Name,
                    Email = userDto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
                    CreatedAt = DateTime.UtcNow,
                    IsAdmin = false
                };

                _logger.LogInformation("Adicionando novo usuário: {UserData}", new { user.Username, user.Email });
                _context.Users.Add(user);
                
                try {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Usuário salvo com sucesso, ID: {UserId}", user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro detalhado ao salvar usuário no banco: {Message}", ex.Message);
                    if (ex.InnerException != null)
                    {
                        _logger.LogError("Inner exception: {Message}", ex.InnerException.Message);
                    }
                    throw;
                }

                var token = GenerateJwtToken(user);

                _logger.LogInformation("Token gerado para o usuário {Email}", user.Email);
                
                return new AuthResult
                {
                    Success = true,
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar usuário: {Message}", ex.Message);
                return new AuthResult { Success = false, Message = "Erro ao registrar usuário: " + ex.Message };
            }
        }

        public async Task<AuthResult> LoginAsync(UserLoginDto userDto)
        {
            try
            {
                _logger.LogInformation("Tentativa de login para usuário: {Email}", userDto.Email);
                
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
                if (user == null)
                {
                    _logger.LogWarning("Usuário não encontrado: {Email}", userDto.Email);
                    return new AuthResult { Success = false, Message = "Usuário não encontrado" };
                }

                if (!BCrypt.Net.BCrypt.Verify(userDto.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Senha incorreta para o usuário: {Email}", userDto.Email);
                    return new AuthResult { Success = false, Message = "Senha incorreta" };
                }

                var token = GenerateJwtToken(user);
                _logger.LogInformation("Login bem-sucedido para o usuário: {Email}", user.Email);

                return new AuthResult
                {
                    Success = true,
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer login: {Message}", ex.Message);
                return new AuthResult { Success = false, Message = "Erro ao fazer login: " + ex.Message };
            }
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<bool> IsUsernameUniqueAsync(string username)
        {
            return !await _context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<bool> IsEmailUniqueAsync(string email)
        {
            return !await _context.Users.AnyAsync(u => u.Email == email);
        }

        private string GenerateJwtToken(User user)
        {
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found"));
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.Username)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public UserDto? User { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
} 