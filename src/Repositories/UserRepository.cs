using IdentityService.Api.Data;
using IdentityService.Api.Interfaces.Repositories;
using IdentityService.Api.Models.User;
using MassTransit;
using Microsoft.EntityFrameworkCore;
namespace IdentityService.Api.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        ApplicationDbContext ApplicationDbContext { get; set; }
        public UserRepository(ApplicationDbContext context) : base(context) 
        {
            ApplicationDbContext = context;
        }
        public  Task<bool> IsUserExistsByEmailAsync(string email)
        {
            return ApplicationDbContext.Set<User>().Where(u => u.Email.ToLower() == email.ToLower()).AnyAsync();
        }
        public  Task<User?> GetByEmailAsync(string email)
        {
            return ApplicationDbContext.Set<User>().Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefaultAsync();
        }
        public  IQueryable<User> GetAllWithRoles()
        {
            return ApplicationDbContext.Set<User>();
        }

        public async Task<User?> GetWithRoleAsync(string email = null, Guid? id = null)
        {
            if (id != null)
            {
                return await ApplicationDbContext.Users.Include(u => u.Role).Where(u => u.Id == id).FirstOrDefaultAsync();

            }
            if (email != null)
            {
                return  await ApplicationDbContext.Users.Include(u => u.Role).AsNoTracking().Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefaultAsync();
            }
            return null;
        }

        public void GenerateResetPasswordToken(User user)
        {
            user.ResetPasswordToken = Guid.NewGuid();
            user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(1);
        }


        public Task<User?> GetByResetPasswordTokenAsync(Guid token)
        {
            return ApplicationDbContext.Set<User>().Where(u => u.ResetPasswordToken == token).FirstOrDefaultAsync();
        }

        public void SetNewPassword(User user, string newPasswordHash)
        {
            user.PasswordHash = newPasswordHash;
            user.LastPasswordChangedAt = DateTime.UtcNow;
            if(user.Status == (char)Enums.Status.New)
            {
                user.Status = (char)Enums.Status.Approved;
                user.ConfirmationToken= null;
            }
            user.ResetPasswordToken = null;
            user.ResetPasswordTokenExpiry = null;

        }

        public void ChangeRole(Guid userId, Guid newRoleId)
        {
            var user = ApplicationDbContext.Set<User>().Where(u => u.Id == userId).FirstOrDefault();
            if (user != null)
            {
                user.RoleId = newRoleId;
            }
        }

    }
}
