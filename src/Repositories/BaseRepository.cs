using IdentityService.Api.Data;
using IdentityService.Api.Interfaces.Models;
using IdentityService.Api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
namespace IdentityService.Api.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class, IModelWithNameAndId
    {
        private readonly ApplicationDbContext _dBContext;


        public BaseRepository(ApplicationDbContext dBContext)
        {
            _dBContext = dBContext;
        }
        public async Task AddAsync(T model)
        {
            await _dBContext.Set<T>().AddAsync(model);

        }

        public async Task DeleteAsync(Guid id)
        {

            var entity = await _dBContext.Set<T>().FirstOrDefaultAsync(x => x.Id == id);

            if (entity != null)
            {
                _dBContext.Set<T>().Remove(entity);
            }
        }
        public async Task<IQueryable<T>> GetAllAsync()
        {
            return  _dBContext.Set<T>();
        }

        public async Task<T?> GetByIdAsync(Guid id)
        {
            return await _dBContext.Set<T>().Where(x => x.Id == id).FirstOrDefaultAsync();
        }
        public async Task<T?> GetByNameAsync(string name)
        {
            return await _dBContext.Set<T>().Where(x => x.Name == name).FirstOrDefaultAsync();
        }
        public Task UpdateAsync(T model)
        {
            _dBContext.Set<T>().Update(model);
            return Task.CompletedTask;

        }
        public async Task<bool> IsNameUniqueAsync(string name)
        {
            return !await _dBContext.Set<T>().AnyAsync(x => x.Name == name);
        }
        public bool IsNameUnique(string name)
        {
            return ! _dBContext.Set<T>().Any(x => x.Name == name);
        }
    }
}
