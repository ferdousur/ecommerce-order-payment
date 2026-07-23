

using ECommerce.Application.DTOs;

namespace ECommerce.Application.Interfaces;

public interface IUserService
{
    Task<AuthResponse> RegisterUserAsync(RegisterDto dto);
    Task<AuthResponse> LoginAsync(LoginDto dto);

}