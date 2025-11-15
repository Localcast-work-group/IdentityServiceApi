using IdentityService.Api.Interfaces.Services;
using IdentityService.Api.Models.Role.DTOs;
using IdentityService.Api.Services;
using FluentValidation;

namespace IdentityService.Api.Models.Role.Validators
{
    public class CreateRoleDTOValidator : AbstractValidator<CreateRoleDTO>
    {
        public CreateRoleDTOValidator(IRoleService roleService)
        {
            RuleFor(x => x.Name).Must( x=> roleService.IsNameUnique(x)).WithMessage("Name must be unique");
        }
    }
}
