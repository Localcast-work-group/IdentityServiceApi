using System.Security.Claims;
using FluentAssertions;
using IdentityService.Api.Exceptions;
using IdentityService.Api.Interfaces;
using IdentityService.Api.Interfaces.Repositories; 
using IdentityService.Api.Interfaces.Services;
using IdentityService.Api.Models.ApiClient;
using IdentityService.Api.Services;
using Moq;

namespace IdentityService.UnitTests.Services
{
    public class OAuthServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IApiClientRepository> _apiClientRepoMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<Serilog.ILogger> _loggerMock;

        private readonly OAuthService _service;

        public OAuthServiceTests()
        {
            _uowMock = new Mock<IUnitOfWork>();
            _apiClientRepoMock = new Mock<IApiClientRepository>();
            _jwtServiceMock = new Mock<IJwtService>();
            _loggerMock = new Mock<Serilog.ILogger>();

            _uowMock.Setup(x => x.ApiClients).Returns(_apiClientRepoMock.Object);

            _service = new OAuthService(
                _uowMock.Object,
                _jwtServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task AuthenticateClientAsync_ShouldReturnToken_WhenCredentialsAreValid()
        {
            // 1. ARRANGE
            var clientId = "video-service";
            var rawSecret = "super-secret-password";

            var secretHash = BCrypt.Net.BCrypt.HashPassword(rawSecret);

            var validClient = new ApiClient
            {
                ClientId = clientId,
                ClientSecretHash = secretHash,
                IsActive = true,
                Name = "Video Service"
            };

            _apiClientRepoMock.Setup(x => x.GetByClientIdAsync(clientId))
                .ReturnsAsync(validClient);

            var expectedToken = "valid.jwt.token";
            _jwtServiceMock.Setup(x => x.GenerateServiceToken(It.IsAny<IEnumerable<Claim>>()))
                .ReturnsAsync(expectedToken);

            // 2. ACT
            var result = await _service.AuthenticateClientAsync(clientId, rawSecret);

            // 3. ASSERT
            result.Should().Be(expectedToken);

            _jwtServiceMock.Verify(x => x.GenerateServiceToken(It.Is<IEnumerable<Claim>>(claims =>
                claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "ServiceApplication") &&
                claims.Any(c => c.Type == ClaimTypes.Name && c.Value == "Video Service")
            )), Times.Once);
        }

        [Fact]
        public async Task AuthenticateClientAsync_ShouldThrowException_WhenClientDoesNotExist()
        {
            // 1. ARRANGE
            var clientId = "unknown-service";

            _apiClientRepoMock.Setup(x => x.GetByClientIdAsync(clientId))
                .ReturnsAsync((ApiClient?)null);

            // 2. ACT
            Func<Task> action = async () => await _service.AuthenticateClientAsync(clientId, "any-secret");

            // 3. ASSERT
            await action.Should().ThrowAsync<AuthenticationFailedException>()
                .WithMessage("Invalid ClientId or ClientSecret");

            _jwtServiceMock.Verify(x => x.GenerateServiceToken(It.IsAny<IEnumerable<Claim>>()), Times.Never);
        }

        [Fact]
        public async Task AuthenticateClientAsync_ShouldThrowException_WhenSecretIsInvalid()
        {
            // 1. ARRANGE
            var clientId = "video-service";
            var correctSecret = "correct-password";
            var wrongSecret = "wrong-password";

            var validClient = new ApiClient
            {
                ClientId = clientId,
                ClientSecretHash = BCrypt.Net.BCrypt.HashPassword(correctSecret),
                IsActive = true
            };

            _apiClientRepoMock.Setup(x => x.GetByClientIdAsync(clientId))
                .ReturnsAsync(validClient);

            // 2. ACT
            Func<Task> action = async () => await _service.AuthenticateClientAsync(clientId, wrongSecret);

            // 3. ASSERT
            await action.Should().ThrowAsync<AuthenticationFailedException>()
                .WithMessage("Invalid ClientId or ClientSecret");

            _jwtServiceMock.Verify(x => x.GenerateServiceToken(It.IsAny<IEnumerable<Claim>>()), Times.Never);
        }

        [Fact]
        public async Task AuthenticateClientAsync_ShouldThrowException_WhenClientIsDeactivated()
        {
            // 1. ARRANGE
            var clientId = "video-service";
            var rawSecret = "super-secret-password";

            var deactivatedClient = new ApiClient
            {
                ClientId = clientId,
                ClientSecretHash = BCrypt.Net.BCrypt.HashPassword(rawSecret),
                IsActive = false
            };

            _apiClientRepoMock.Setup(x => x.GetByClientIdAsync(clientId))
                .ReturnsAsync(deactivatedClient);

            // 2. ACT
            Func<Task> action = async () => await _service.AuthenticateClientAsync(clientId, rawSecret);

            // 3. ASSERT
            await action.Should().ThrowAsync<AuthenticationFailedException>()
                .WithMessage("Client is deactivated");

            _jwtServiceMock.Verify(x => x.GenerateServiceToken(It.IsAny<IEnumerable<Claim>>()), Times.Never);
        }
    }
}