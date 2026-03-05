using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace LinkGuardiao.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository users,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService,
            ILogger<UserService> logger)
        {
            _users = users;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        public async Task<AuthResult> RegisterAsync(UserRegisterDto userDto)
        {
            if (userDto == null)
            {
                _logger.LogWarning("UserDto is null");
                return new AuthResult { Success = false, Message = "Dados de usuário inválidos" };
            }

            var normalizedEmail = userDto.Email.Trim().ToLowerInvariant();
            var normalizedName = userDto.Name.Trim();

            if (await _users.EmailExistsAsync(normalizedEmail))
            {
                _logger.LogWarning("Registration blocked for existing email {Email}", normalizedEmail);
                return new AuthResult { Success = false, Message = "E-mail já cadastrado" };
            }

            var user = new User
            {
                Id = Guid.NewGuid().ToString("N"),
                Username = normalizedName,
                Email = normalizedEmail,
                PasswordHash = _passwordHasher.Hash(userDto.Password),
                CreatedAt = DateTime.UtcNow,
                IsAdmin = false
            };
            await _users.CreateAsync(user);

            var token = _jwtTokenService.GenerateToken(user);

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

        public async Task<AuthResult> LoginAsync(UserLoginDto userDto)
        {
            if (userDto == null)
            {
                _logger.LogWarning("Login payload is null");
                return new AuthResult { Success = false, Message = "Dados de login inválidos" };
            }

            var normalizedEmail = userDto.Email.Trim().ToLowerInvariant();
            var user = await _users.GetByEmailAsync(normalizedEmail);
            if (user == null || !_passwordHasher.Verify(userDto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid login attempt for {Email}", normalizedEmail);
                return new AuthResult { Success = false, Message = "Credenciais inválidas" };
            }

            var token = _jwtTokenService.GenerateToken(user);

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

        public Task<User?> GetByIdAsync(string id)
        {
            return _users.GetByIdAsync(id);
        }
        public Task<bool> IsUsernameUniqueAsync(string username)
        {
            return Task.FromResult(true);
        }

        public async Task<bool> IsEmailUniqueAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return !await _users.EmailExistsAsync(normalizedEmail);
        }
    }
}
