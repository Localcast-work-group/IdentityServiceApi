using IdentityService.Api.Data;
using IdentityService.Api.Interfaces.Repositories;
using IdentityService.Api.Interfaces.Services;
using IdentityService.Api.Models.Role;
using IdentityService.Api.Repositories;

namespace IdentityService.Api.Services
{
    public class RoleRepository : BaseRepository<Role>, IRoleRepository
    {
        public RoleRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext) { }
    }
}
