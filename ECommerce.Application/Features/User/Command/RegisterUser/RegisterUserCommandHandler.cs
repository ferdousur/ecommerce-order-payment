using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ErrorOr;

namespace ECommerce.Application.Features.User.Command.RegisterUser;

public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, ErrorOr<AuthResponse>>
{
    private readonly IUserService _userService;
    public RegisterUserCommandHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<ErrorOr<AuthResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var dto = new RegisterDto
        (
            request.FirstName,
            request.LastName,
            request.UserName,
            request.Email,
            request.Password
        );

        var result = await _userService.RegisterUserAsync(dto);

        return result;
    }
}