using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Entities;

namespace LinkGuardiao.Application.Interfaces
{
    public interface IUserService
    {
        Task<AuthResult> RegisterAsync(UserRegisterDto userDto);
        Task<AuthResult> LoginAsync(UserLoginDto userDto);
        Task<AuthResult> RefreshAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
        Task<User?> GetByIdAsync(string id);
        Task<bool> IsUsernameUniqueAsync(string username);
        Task<bool> IsEmailUniqueAsync(string email);
    }
}
