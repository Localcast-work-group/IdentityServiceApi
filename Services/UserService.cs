using BCrypt;
using Humanizer;
using IdentityService.Api.Enums;
using IdentityService.Api.Exceptions;
using IdentityService.Api.Exceptions.BusinessRuleValidation;
using IdentityService.Api.Interfaces;
using IdentityService.Api.Interfaces.Services;
using IdentityService.Api.Models.RefreshToken;
using IdentityService.Api.Models.Role;
using IdentityService.Api.Models.User;
using IdentityService.Api.Models.User.DTOs;
using MassTransit;
using System.Security.Claims;
namespace IdentityService.Api.Services
{
    public class UserService : BaseService<User>,IUserService
    {
        IRoleService RoleService { get; set; }
        IUnitOfWork UnitOfWork { get; set; }
        Serilog.ILogger Logger { get; set; }
        IPublishEndpoint PublishEndpoint { get; set; }

        IJwtService JwtService { get; set; }
        public UserService( IRoleService roleService, IUnitOfWork unitOfWork,IJwtService jwtService, IPublishEndpoint publishEndpoint, Serilog.ILogger logger) : base(unitOfWork) 
        {
            RoleService = roleService;
            UnitOfWork = unitOfWork;
            JwtService = jwtService;
            PublishEndpoint = publishEndpoint;
            Logger = logger;
        }

        public async Task<User?> RegisterUser(RegisterUserDTO model)
        {
            Guid? roleId = null;
            Role? role = (await RoleService.GetByName("User"));
            string roleName =  "";
            if (role == null)
            {
                Role newRole = new Role
                {
                    Name = "User"
                };
                roleName = newRole.Name;
                await RoleService.Add(newRole);
                roleId = newRole.Id;
            }
            else
            {
                roleId = role.Id;
                roleName = role.Name;
            }
            if (await UnitOfWork.Users.IsUserExistsByEmailAsync(model.Email))
            {
                throw new DuplicateFieldException($"Email '{model.Email}' is already taken.", "Email");
            }

            if (await UnitOfWork.Users.IsNameUniqueAsync(model.Name) == false)
            {
                throw new DuplicateFieldException($"Name '{model.Name}' is already taken.", "Name");
            }
            User user = new User
            {
                CreatedAt = DateTime.UtcNow,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Name = model.Name,
                Surname = model.Surname,
                FirstName = model.FirstName,
                RoleId = roleId.Value,
                Status = (char)Status.New,
                ConfirmationToken = Guid.NewGuid()
            };
            
            await UnitOfWork.Users.AddAsync(user);
            await PublishEndpoint.Publish<IdentityService.Contracts.Events.UserCreatedEvent>(new
            {
                Id = user.Id,
                Name = user.Name,
                FirstName = user.FirstName,
                Surname = user.Surname,
                Email = user.Email,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                RoleName = roleName,
                ConfirmationToken = user.ConfirmationToken
            });
            await UnitOfWork.SaveChangesAsync();
            Logger.Information("Published UserCreatedEvent for user {UserId} ({UserEmail})", user.Id, user.Email);

            return user;
        }
        public async Task<User?> RegisterAdmin()
        {
            Guid? roleId = null;
            Role? role = (await RoleService.GetByName("Admin"));
            if (role == null)
            {
                Role newRole = new Role
                {
                    Name = "Admin"
                };
                await RoleService.Add(newRole);
                roleId = newRole.Id;
            }
            else
            {
                roleId = role.Id;
            }
            User? existing = await UnitOfWork.Users.GetByEmailAsync("test@admin.pl");
            if (existing != null)
            {
                return existing;
            }
            User user = new User
            { 
                CreatedAt = DateTime.UtcNow,
                Email = "test@admin.pl",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Name = "Admin",
                Surname = "Andrzej",
                FirstName = "Testowy",
                RoleId = roleId.Value,
                Status = (char)Status.New

            };
            await UnitOfWork.Users.AddAsync(user);
            await PublishEndpoint.Publish<IdentityService.Contracts.Events.UserCreatedEvent>(new
            {
                Id = user.Id,
                Name = user.Name,
                FirstName = user.FirstName,
                Surname = user.Surname,
                Email = user.Email,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                RoleName = "Admin",
            });
            Logger.Information("Published UserCreatedEvent for user {UserId} ({UserEmail})", user.Id, user.Email);
            await UnitOfWork.SaveChangesAsync();
            return user;
        }
        public async Task<bool> IsUserExistsByEmail(string email)
        {
            return await UnitOfWork.Users.IsUserExistsByEmailAsync(email);
        }
        public async Task<User?> GetByEmail(string email)
        {
            return await UnitOfWork.Users.GetByEmailAsync(email);
        }
        public async Task<User?> GetWithRole(string email = null, Guid? id = null)
        {
           return await UnitOfWork.Users.GetWithRoleAsync(email, id);
        }
        public async Task<(string JwtToken, string RefreshToken)> HandleLoginAsync(LoginUserDto loginUserDto , string ipAdress)
        {
            User user = await UnitOfWork.Users.GetWithRoleAsync(email:loginUserDto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginUserDto.Password, user.PasswordHash))
            {
                throw new AuthenticationFailedException("Invalid email or password");
            }

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role.Name)
            };
            
            Logger.Information("Handling user log in {UserId} ({UserEmail}) Ip: {ipAdress}", user.Id, user.Email,ipAdress);
            (string jwt, string refreshToken) = await JwtService.GenerateTokens(user.Id, claims);
            await UnitOfWork.SaveChangesAsync();
            return (jwt, refreshToken);
        }
        public async Task<(string JwtToken, string RefreshToken)> HandleTokenRefreshAsync(Guid userId, string ipAdress)
        {
            // some logic like logging, maybe queue in fututre

            User user = await UnitOfWork.Users.GetWithRoleAsync(id:userId);
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role.Name)
            };
            Logger.Information("Handling token refresh for user {UserId} ({UserEmail}) Ip: {ipAdress}", user.Id, user.Email, ipAdress);
            (string jwt, string refreshToken) = await JwtService.GenerateTokens(user.Id, claims);
            await UnitOfWork.SaveChangesAsync();
            return (jwt, refreshToken);
        }
        public async Task GenerateResetPasswordToken(User user)
        {
            await UnitOfWork.Users.GenerateResetPasswordTokenAsync(user);
            await PublishEndpoint.Publish<IdentityService.Contracts.Events.ResetPasswordTokenGeneratedEvent>(new
            {
                UserId = user.Id,
                Email = user.Email,
                ResetPasswordToken = user.ResetPasswordToken,
                ResetPasswordTokenExpiry = user.ResetPasswordTokenExpiry,
                UserName = user.Name
            });
            Logger.Information("Published ResetPasswordTokenGeneratedEvent for user {UserId} ({UserEmail})", user.Id, user.Email);
            await UnitOfWork.SaveChangesAsync();
        }
        public async Task ResetPassword(Guid token, string newPassword)
        {
            User? user = await UnitOfWork.Users.GetByResetPasswordTokenAsync(token);
            if (user == null  || user.ResetPasswordTokenExpiry < DateTime.UtcNow)
            {
                throw new InvalidTokenException("Invalid token");
            }
            string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await UnitOfWork.Users.SetNewPassword(user, newPasswordHash);
            await JwtService.RevokeRefreshTokensForUser(user.Id);
            await PublishEndpoint.Publish<IdentityService.Contracts.Events.PasswordChangedEvent>(new
            {
                UserId = user.Id,
                ChangeDate = DateTime.UtcNow,
                ForceRefreshTokens = true
            });
            Logger.Information("Published PasswordChangedEvent for user {UserId} ({UserEmail})", user.Id, user.Email);
            await UnitOfWork.SaveChangesAsync();
        }

    }
}
