using IdentityService.Api.Models.User.DTOs;
using FluentValidation;

namespace IdentityService.Api.Models.User.Validators
{
    public class RegisterUserDTOValidator : AbstractValidator<RegisterUserDTO>
    {
        public RegisterUserDTOValidator() 
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Email).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Email).EmailAddress();
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6).Equal(x => x.ConfirmPassword).WithMessage("Passwords are not equals");
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Surname).NotEmpty().MaximumLength(150);
            
        }
    }
}
