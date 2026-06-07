using IdentityService.Api.Interfaces.Models;

namespace IdentityService.Api.Interfaces.Services
{
    public interface IBaseService<T> where T : IModelWithNameAndId
    {
        public Task<T?> GetById(Guid id);
        public Task<T?> GetByName(string name);
        public IQueryable<T> GetAll();
        public Task<Guid> Add(T model);
        public Task<Guid> Update(T model);
        public Task Delete(Guid id);
        public Task<bool> IsNameUniqueAsync(string name);
        public bool IsNameUnique(string name);

    }
}
