using IdentityService.Api.Models.User;
using IdentityService.Api.Models.User.DTOs;

namespace IdentityService.Api.Interfaces.Services
{
    public interface IUserService : IBaseService<User>
    {
        public Task<User?> RegisterUser(RegisterUserDTO model);
        public Task<User?> RegisterAdmin();
        public Task<bool> IsUserExistsByEmail(string email);
        public Task<User?> GetByEmail(string email);
        public Task<User?> GetWithRole(string email = null, Guid? id = null);
        public Task GenerateResetPasswordToken(User user);
        public Task<(string JwtToken, string RefreshToken)> HandleLoginAsync(LoginUserDto loginUserDto);
        public Task<(string JwtToken, string RefreshToken)> HandleTokenRefreshAsync(Guid userId);
        public Task ResetPassword(Guid token, string newPassword);
    }
}
