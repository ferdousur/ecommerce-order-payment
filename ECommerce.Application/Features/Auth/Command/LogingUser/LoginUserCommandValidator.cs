using ECommerce.Application.Features.Auth.Command.LoginUser;
using FluentValidation;

namespace ECommerce.Application.Features.Auth.Command.LoginUser;


public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username or email is required");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}