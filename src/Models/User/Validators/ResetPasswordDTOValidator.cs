using IdentityService.Api.Models.User.DTOs;
using FluentValidation;

namespace IdentityService.Api.Models.User.Validators
{
    public class ResetPasswordDTOValidator : AbstractValidator<ResetPasswordDTO>
    {
        public ResetPasswordDTOValidator()
        {
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6).Equal(x => x.ConfirmPassword).WithMessage("Passwords are not equals");
            RuleFor(x => x.ResetPasswordToken).NotEmpty();

        }
    }
}
