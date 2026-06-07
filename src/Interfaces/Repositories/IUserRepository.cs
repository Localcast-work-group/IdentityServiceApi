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
        public IQueryable<User> GetAllWithRoles();
        public void SetNewPassword(User user, string newPasswordHash);
        public void GenerateResetPasswordToken(User user);
        public void ChangeRole(Guid userId, Guid roleId);
    }
}
