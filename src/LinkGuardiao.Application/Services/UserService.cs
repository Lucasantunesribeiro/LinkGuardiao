using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LinkGuardiao.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IApplicationDbContext context,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService,
            ILogger<UserService> logger)
        {
            _context = context;
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

            var userExists = await _context.Users.AnyAsync(u => u.Email == normalizedEmail);
            if (userExists)
            {
                return new AuthResult { Success = false, Message = "E-mail já cadastrado" };
            }

            var user = new User
            {
                Username = normalizedName,
                Email = normalizedEmail,
                PasswordHash = _passwordHasher.Hash(userDto.Password),
                CreatedAt = DateTime.UtcNow,
                IsAdmin = false
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

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
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            if (user == null || !_passwordHasher.Verify(userDto.Password, user.PasswordHash))
            {
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
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return !await _context.Users.AnyAsync(u => u.Email == normalizedEmail);
        }
    }
}
