using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkGuardiao.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _users;
        private readonly IRefreshTokenRepository _refreshTokens;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository users,
            IRefreshTokenRepository refreshTokens,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService,
            IOptions<JwtOptions> jwtOptions,
            ILogger<UserService> logger)
        {
            _users = users;
            _refreshTokens = refreshTokens;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
            _jwtOptions = jwtOptions.Value;
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
            var (rawRefresh, refreshEntity) = await CreateRefreshTokenAsync(user.Id);

            return new AuthResult
            {
                Success = true,
                Token = token,
                RefreshToken = rawRefresh,
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
            var (rawRefresh, _) = await CreateRefreshTokenAsync(user.Id);

            return new AuthResult
            {
                Success = true,
                Token = token,
                RefreshToken = rawRefresh,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email
                }
            };
        }

        public async Task<AuthResult> RefreshAsync(string refreshToken)
        {
            var hash = _jwtTokenService.HashToken(refreshToken);
            var stored = await _refreshTokens.GetByTokenHashAsync(hash);

            if (stored == null || stored.IsRevoked || stored.ExpiresAt <= DateTime.UtcNow)
            {
                return new AuthResult { Success = false, Message = "Refresh token inválido ou expirado" };
            }

            var user = await _users.GetByIdAsync(stored.UserId);
            if (user == null)
            {
                return new AuthResult { Success = false, Message = "Usuário não encontrado" };
            }

            await _refreshTokens.RevokeAsync(hash);
            var newAccessToken = _jwtTokenService.GenerateToken(user);
            var (newRawRefresh, _) = await CreateRefreshTokenAsync(user.Id);

            return new AuthResult
            {
                Success = true,
                Token = newAccessToken,
                RefreshToken = newRawRefresh,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email
                }
            };
        }

        public async Task LogoutAsync(string refreshToken)
        {
            var hash = _jwtTokenService.HashToken(refreshToken);
            await _refreshTokens.RevokeAsync(hash);
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

        private async Task<(string rawToken, RefreshToken entity)> CreateRefreshTokenAsync(string userId)
        {
            var rawToken = _jwtTokenService.GenerateRefreshToken();
            var hash = _jwtTokenService.HashToken(rawToken);
            var entity = new RefreshToken
            {
                TokenHash = hash,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
                IsRevoked = false
            };
            await _refreshTokens.CreateAsync(entity);
            return (rawToken, entity);
        }
    }
}
