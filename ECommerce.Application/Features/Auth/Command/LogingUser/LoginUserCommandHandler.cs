using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ErrorOr;

namespace ECommerce.Application.Features.Auth.Command.LoginUser;

public class LogingUserCommandHandler : ICommandHandler<LoginUserCommand, ErrorOr<AuthResponse>>
{
    private readonly IUserService _userService;
    public LogingUserCommandHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<ErrorOr<AuthResponse>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var dto = new LoginDto
        (
            request.UserName, request.Password
        );

        var result = await _userService.LoginAsync(dto);

        return result;
    }
}