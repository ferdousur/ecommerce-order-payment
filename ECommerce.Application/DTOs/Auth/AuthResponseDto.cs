
namespace ECommerce.Application.DTOs;

public record class AuthResponse(string UserName, string role, string token, DateTime TokenExpiresAt);