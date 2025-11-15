using IdentityService.Api.Data;
using IdentityService.Api.Interfaces;
using IdentityService.Api.Interfaces.Repositories;
using IdentityService.Api.Interfaces.Services;
using IdentityService.Api.Models.Role;

namespace IdentityService.Api.Services
{
    public class RoleService : BaseService<Role>, IRoleService
    {
        public RoleService(IUnitOfWork unitOfWork) : base(unitOfWork) { }
    }
}
