using System.Security.Claims;
using FluentAssertions;
using IdentityService.Api.Enums;
using IdentityService.Api.Exceptions;
using IdentityService.Api.Exceptions.BusinessRuleValidation;
using IdentityService.Api.Interfaces;
using IdentityService.Api.Interfaces.Repositories; // Zakładam namespace repozytoriów
using IdentityService.Api.Interfaces.Services;
using IdentityService.Api.Models.Role;
using IdentityService.Api.Models.User;
using IdentityService.Api.Models.User.DTOs;
using IdentityService.Api.Services;
using IdentityService.Contracts.Events;
using MassTransit;
using Moq;
using Xunit;

namespace IdentityService.UnitTests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IRoleService> _roleServiceMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<IPublishEndpoint> _publishEndpointMock;
        private readonly Mock<IUserContext> _userContextMock;
        private readonly Mock<Serilog.ILogger> _loggerMock;

        private readonly Mock<IUserRepository> _userRepoMock;

        private readonly UserService _service;

        public UserServiceTests()
        {
            _uowMock = new Mock<IUnitOfWork>();
            _roleServiceMock = new Mock<IRoleService>();
            _jwtServiceMock = new Mock<IJwtService>();
            _publishEndpointMock = new Mock<IPublishEndpoint>();
            _userContextMock = new Mock<IUserContext>();
            _loggerMock = new Mock<Serilog.ILogger>();
            _userRepoMock = new Mock<IUserRepository>();

            _uowMock.Setup(x => x.Users).Returns(_userRepoMock.Object);

            _userContextMock.Setup(x => x.IpAddress).Returns("127.0.0.1");

            _service = new UserService(
                _roleServiceMock.Object,
                _uowMock.Object,
                _jwtServiceMock.Object,
                _publishEndpointMock.Object,
                _loggerMock.Object,
                _userContextMock.Object
            );
        }


        [Fact]
        public async Task RegisterUser_ShouldCreateUserAndPublishEvent_WhenDataIsValid()
        {
            // 1. ARRANGE
            var dto = new RegisterUserDTO
            {
                Email = "new@test.com",
                Name = "newuser",
                Password = "password123",
                FirstName = "John",
                Surname = "Doe"
            };

            _userRepoMock.Setup(x => x.IsUserExistsByEmailAsync(dto.Email)).ReturnsAsync(false);
            _userRepoMock.Setup(x => x.IsNameUniqueAsync(dto.Name)).ReturnsAsync(true);

            var existingRole = new Role { Id = Guid.NewGuid(), Name = "User" };
            _roleServiceMock.Setup(x => x.GetByName("User")).ReturnsAsync(existingRole);

            // 2. ACT
            var result = await _service.RegisterUser(dto);

            // 3. ASSERT
            result.Should().NotBeNull();
            result.Email.Should().Be(dto.Email);
            result.RoleId.Should().Be(existingRole.Id);
            result.Status.Should().Be((char)Status.New);

            result.PasswordHash.Should().NotBe(dto.Password);
            BCrypt.Net.BCrypt.Verify(dto.Password, result.PasswordHash).Should().BeTrue();

            _userRepoMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
            _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _publishEndpointMock.Verify(x => x.Publish(It.Is<UserCreatedEvent>(e =>
                e.Email == dto.Email && e.Name == dto.Name
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RegisterUser_ShouldThrowDuplicateException_WhenEmailExists()
        {
            // 1. ARRANGE
            var dto = new RegisterUserDTO { Email = "exists@test.com" };
            _userRepoMock.Setup(x => x.IsUserExistsByEmailAsync(dto.Email)).ReturnsAsync(true);

            // 2. ACT
            Func<Task> action = async () => await _service.RegisterUser(dto);

            // 3. ASSERT
            await action.Should().ThrowAsync<DuplicateFieldException>()
                .WithMessage($"Email '{dto.Email}' is already taken.");

            _userRepoMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RegisterUser_ShouldCreateRole_WhenUserRoleDoesNotExist()
        {
            // 1. ARRANGE
            var dto = new RegisterUserDTO { Email = "a@a.com", Name = "a", Password = "p" };

            _userRepoMock.Setup(x => x.IsUserExistsByEmailAsync(dto.Email)).ReturnsAsync(false);
            _userRepoMock.Setup(x => x.IsNameUniqueAsync(dto.Name)).ReturnsAsync(true);

            _roleServiceMock.Setup(x => x.GetByName("User")).ReturnsAsync((Role?)null);

            // 2. ACT
            await _service.RegisterUser(dto);

            // 3. ASSERT
            _roleServiceMock.Verify(x => x.Add(It.Is<Role>(r => r.Name == "User")), Times.Once);
        }


        [Fact]
        public async Task HandleLoginAsync_ShouldReturnTokens_WhenCredentialsAreCorrect()
        {
            // 1. ARRANGE
            var loginDto = new LoginUserDto { Email = "user@test.com", Password = "correctPass" };
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Email = loginDto.Email,
                Name = "TestUser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(loginDto.Password),
                Role = new Role { Name = "User" }
            };

            _userRepoMock.Setup(x => x.GetWithRoleAsync(loginDto.Email, null)).ReturnsAsync(user);

            _jwtServiceMock.Setup(x => x.GenerateTokens(userId, It.IsAny<IEnumerable<Claim>>()))
                .ReturnsAsync(("valid_jwt", "valid_refresh"));

            // 2. ACT
            var (jwt, refresh) = await _service.HandleLoginAsync(loginDto);

            // 3. ASSERT
            jwt.Should().Be("valid_jwt");
            refresh.Should().Be("valid_refresh");

            _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleLoginAsync_ShouldThrowAuthenticationFailed_WhenPasswordIsWrong()
        {
            // 1. ARRANGE
            var loginDto = new LoginUserDto { Email = "user@test.com", Password = "wrongPass" };

            var user = new User
            {
                Email = loginDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctPass")
            };

            _userRepoMock.Setup(x => x.GetWithRoleAsync(loginDto.Email, null)).ReturnsAsync(user);

            // 2. ACT
            Func<Task> action = async () => await _service.HandleLoginAsync(loginDto);

            // 3. ASSERT
            await action.Should().ThrowAsync<AuthenticationFailedException>()
                .WithMessage("Invalid email or password");
        }


        [Fact]
        public async Task ResetPassword_ShouldChangePassword_AndRevokeTokens_WhenTokenIsValid()
        {
            // 1. ARRANGE
            var token = Guid.NewGuid();
            var newPassword = "newPassword123";
            var user = new User
            {
                Id = Guid.NewGuid(),
                ResetPasswordTokenExpiry = DateTime.UtcNow.AddMinutes(15) // Ważny token
            };

            _userRepoMock.Setup(x => x.GetByResetPasswordTokenAsync(token)).ReturnsAsync(user);

            // 2. ACT
            await _service.ResetPassword(token, newPassword);

            // 3. ASSERT
            _userRepoMock.Verify(x => x.SetNewPassword(user, It.IsAny<string>()), Times.Once);

            _jwtServiceMock.Verify(x => x.RevokeRefreshTokensForUser(user.Id), Times.Once);

            _publishEndpointMock.Verify(x => x.Publish(It.Is<PasswordChangedEvent>(e =>
                e.UserId == user.Id && e.ForceRefreshTokens == true
            ), It.IsAny<CancellationToken>()), Times.Once);

            _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ResetPassword_ShouldThrowInvalidToken_WhenTokenIsExpired()
        {
            // 1. ARRANGE
            var token = Guid.NewGuid();
            var user = new User
            {
                ResetPasswordTokenExpiry = DateTime.UtcNow.AddMinutes(-5)
            };

            _userRepoMock.Setup(x => x.GetByResetPasswordTokenAsync(token)).ReturnsAsync(user);

            // 2. ACT
            Func<Task> action = async () => await _service.ResetPassword(token, "newPass");

            // 3. ASSERT
            await action.Should().ThrowAsync<InvalidTokenException>();
            _userRepoMock.Verify(x => x.SetNewPassword(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        }
    }
}