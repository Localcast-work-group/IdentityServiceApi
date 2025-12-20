using IdentityService.Api.Models.User;
using IdentityService.Api.Models.User.DTOs;

namespace IdentityService.Api.Interfaces.Repositories
{
    public interface IUserRepository : IBaseRepository<User>
    {
        public Task<bool> IsUserExistsByEmailAsync(string email);
        public Task<User?> GetByEmailAsync(string email);
        public Task<User?> GetByResetPasswordTokenAsync(Guid token);
        public Task<User?> GetWithRoleAsync(string email = null, Guid? id = null);
        public Task<IQueryable<User>> GetAllWithRolesAsync();
        public Task SetNewPassword(User user, string newPasswordHash);
        public Task GenerateResetPasswordTokenAsync(User user);
        public Task ChangeRole(Guid userId, Guid roleId);
    }
}
