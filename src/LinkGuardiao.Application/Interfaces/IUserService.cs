using LinkGuardiao.Application.DTOs;
using LinkGuardiao.Application.Entities;

namespace LinkGuardiao.Application.Interfaces
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
