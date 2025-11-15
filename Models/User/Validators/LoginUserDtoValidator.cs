using IdentityService.Api.Models.User.DTOs;
using FluentValidation;

namespace IdentityService.Api.Models.User.Validators
{
    public class LoginUserDtoValidator : AbstractValidator<LoginUserDto>
    {
        public LoginUserDtoValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();

        }
    }
}
