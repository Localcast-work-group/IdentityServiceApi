using IdentityService.Api.Data;
using IdentityService.Api.Interfaces;
using IdentityService.Api.Interfaces.Models;
using IdentityService.Api.Interfaces.Repositories;
using IdentityService.Api.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
namespace IdentityService.Api.Services
{
    public class BaseService<T> : IBaseService<T> where T : class, IModelWithNameAndId
    {
        private readonly IBaseRepository<T> _baseRepository;
        private readonly IUnitOfWork _unitOfWork;

        public BaseService(IUnitOfWork unitOfWork)
        {
            _baseRepository = unitOfWork.Repository<T>();
            _unitOfWork = unitOfWork;
        }
        public async Task<Guid> Add(T model)
        {
            await _baseRepository.AddAsync(model);
            await _unitOfWork.SaveChangesAsync();
            return model.Id;
        }

        public async Task Delete(Guid id)
        {
            await _baseRepository.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
        }
        public IQueryable<T> GetAll()
        {
            return  _baseRepository.GetAll();
        }

        public async Task<T?> GetById(Guid id)
        {
            return await _baseRepository.GetByIdAsync(id);
        }
        public async Task<T?> GetByName(string name)
        {
            return await _baseRepository.GetByNameAsync(name);
        }
        public async Task<Guid> Update(T model)
        {
             _baseRepository.Update(model);
            await _unitOfWork.SaveChangesAsync();
            return model.Id;
        }
        public bool IsNameUnique(string name)
        {
            return _baseRepository.IsNameUnique(name);
        }
        public Task<bool> IsNameUniqueAsync(string name)
        {
            return _baseRepository.IsNameUniqueAsync(name);
        }
    }
}
