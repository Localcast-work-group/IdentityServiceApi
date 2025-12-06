using IdentityService.Api.Interfaces.Models;

namespace IdentityService.Api.Interfaces.Repositories
{
    public interface IBaseRepository<T> where T : IModelWithNameAndId
    {
        public Task<T?> GetByIdAsync(Guid id);
        public Task<T?> GetByNameAsync(string name);
        public Task<IQueryable<T>> GetAllAsync();
        public Task AddAsync(T model);
        public Task UpdateAsync(T model);
        public Task DeleteAsync(Guid id);
        public Task<bool> IsNameUniqueAsync(string name);
        public bool IsNameUnique(string name);

    }
}
