using IdentityService.Api.Interfaces.Models;
using IdentityService.Api.Interfaces.Repositories;

namespace IdentityService.Api.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IJwtRepository Jwts { get; }
        IRoleRepository Roles { get; }
        IBaseRepository<T> Repository<T>() where T : class, IModelWithNameAndId;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
