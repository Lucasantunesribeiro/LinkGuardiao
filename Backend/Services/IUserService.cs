using LinkGuardiao.API.DTOs;
using LinkGuardiao.API.Models;

namespace LinkGuardiao.API.Services
{
    public interface IUserService
    {
        Task<AuthResult> RegisterAsync(UserRegisterDto userDto);
        Task<AuthResult> LoginAsync(UserLoginDto userDto);
        Task<User?> GetByIdAsync(int id);
        Task<bool> IsUsernameUniqueAsync(string username);
        Task<bool> IsEmailUniqueAsync(string email);
    }
}