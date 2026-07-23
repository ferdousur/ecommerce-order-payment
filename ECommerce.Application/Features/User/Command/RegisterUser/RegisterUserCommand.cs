

using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs;
using ErrorOr;

namespace ECommerce.Application.Features.User.Command.RegisterUser;

public record RegisterUserCommand(
    string FirstName,
    string LastName,
    string UserName,
    string Email,
    string Password) : ICommand<ErrorOr<AuthResponse>>;
