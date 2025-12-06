using IdentityService.Api.Interfaces;
using IdentityService.Api.Interfaces.Models;
using IdentityService.Api.Interfaces.Repositories;
using IdentityService.Api.Repositories;
using IdentityService.Api.Services;
using System.Collections;

namespace IdentityService.Api.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _dbContext;
        private Hashtable _repositories;
        private IUserRepository _userRepository;
        private IJwtRepository _jwtRepository;
        private IRoleRepository _roleRepository;
        private IApiClientRepository _apiClientRepository;

        public UnitOfWork(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IUserRepository Users
        {
            get
            {
                return _userRepository ??= new UserRepository(_dbContext);
            }
        }
        public IJwtRepository Jwts
        {
            get
            {
                return _jwtRepository ??= new JwtRepository(_dbContext);
            }
        }
        public IRoleRepository Roles 
        {
            get
            {
                return _roleRepository ??= new RoleRepository(_dbContext);
            }
        }
        public IApiClientRepository ApiClients
        {
            get
            {
                return _apiClientRepository ??= new ApiClientRepository(_dbContext);
            }
        }
        // for BaseService
        public IBaseRepository<T> Repository<T>() where T : class, IModelWithNameAndId
        {
            if (_repositories == null)
                _repositories = new Hashtable();

            var type = typeof(T).Name;

            if (!_repositories.ContainsKey(type))
            {
                var repositoryType = typeof(BaseRepository<>);
                var repositoryInstance = Activator.CreateInstance(
                    repositoryType.MakeGenericType(typeof(T)), _dbContext);

                _repositories.Add(type, repositoryInstance);
            }

            return (IBaseRepository<T>)_repositories[type];
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
