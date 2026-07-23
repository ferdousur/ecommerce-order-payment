using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs;
using ErrorOr;


namespace ECommerce.Application.Features.Auth.Command.LoginUser;

public record LoginUserCommand(
    string UserName, string Password) : ICommand<ErrorOr<AuthResponse>>;
