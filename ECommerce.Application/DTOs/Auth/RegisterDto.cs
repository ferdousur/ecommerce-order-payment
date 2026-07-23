
namespace ECommerce.Application.DTOs;


public record class RegisterDto(string FirstName, string LastName, string UserName, string Email, string Password);