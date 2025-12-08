using FluentAssertions;
using IdentityService.Api.Data;
using IdentityService.Api.Extensions;
using IdentityService.Api.Interfaces;
using IdentityService.Api.Interfaces.Repositories;
using IdentityService.Api.Interfaces.Services;
using IdentityService.Api.Models.RefreshToken;
using IdentityService.Api.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IdentityService.UnitTests.Services
{
    public class JwtServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IJwtRepository> _jwtRepoMock;
        private readonly Mock<Serilog.ILogger> _loggerMock;
        private readonly Mock<IUserContext> _userContextMock; 
        private readonly AuthenticationSettings _authSettings;
        private readonly JwtService _service;

        public JwtServiceTests()
        {
            _uowMock = new Mock<IUnitOfWork>();
            _jwtRepoMock = new Mock<IJwtRepository>();
            _loggerMock = new Mock<Serilog.ILogger>();
            _userContextMock = new Mock<IUserContext>(); 

            _uowMock.Setup(x => x.Jwts).Returns(_jwtRepoMock.Object);

            _authSettings = new AuthenticationSettings
            {
                Key = "super_secret_key_for_testing_purposes_123",
                Issuer = "IdentityService",
                ExpireMinutes = 60,
                RefreshExpireDays = 7
            };

            _userContextMock.Setup(x => x.IpAddress).Returns("127.0.0.1");

            _service = new JwtService(
                _uowMock.Object,
                _authSettings,
                _loggerMock.Object,
                _userContextMock.Object 
            );
        }

        [Fact]
        public async Task GenerateTokens_ShouldReturnValidTokens_AndSaveRefreshToken()
        {
            // 1. ARRANGE
            var userId = Guid.NewGuid();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "testuser") };

            // 2. ACT
            var (jwt, refreshToken) = await _service.GenerateTokens(userId, claims);

            // 3. ASSERT
            jwt.Should().NotBeNullOrEmpty();
            refreshToken.Should().NotBeNullOrEmpty();

            _jwtRepoMock.Verify(x => x.AddRefreshTokenAsync(It.Is<RefreshToken>(rt =>
                rt.UserId == userId && rt.Token == refreshToken
            )), Times.Once);
        }


        [Fact]
        public async Task ValidateAndRotate_ShouldReturnTrue_WhenTokenIsValid()
        {
            // 1. ARRANGE
            var tokenString = Guid.NewGuid().ToString();
            var userId = Guid.NewGuid();

            var validToken = new RefreshToken
            {
                Token = tokenString,
                UserId = userId,
                IsUsed = false,
                IsRevoked = false,
                ExpiryDate = DateTime.UtcNow.AddDays(1)
            };

            _jwtRepoMock.Setup(x => x.GetRefreshTokenAsync(tokenString)).ReturnsAsync(validToken);

            // 2. ACT
            var result = await _service.ValidateAndRotateRefreshToken(tokenString);

            // 3. ASSERT
            result.IsValid.Should().BeTrue();
            result.UserId.Should().Be(userId);

            _jwtRepoMock.Verify(x => x.MarkRefreshTokenAsUsed(validToken), Times.Once);
            _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(true, false, "2000-01-01", "Used")]     
        [InlineData(false, true, "2100-01-01", "Revoked")]   
        [InlineData(false, false, "2000-01-01", "Expired")]  
        public async Task ValidateAndRotate_ShouldReturnFalse_WhenTokenIsInvalid(bool isUsed, bool isRevoked, string expiryDateStr, string caseName)
        {
            // 1. ARRANGE
            var tokenString = Guid.NewGuid().ToString();
            var invalidToken = new RefreshToken
            {
                Token = tokenString,
                IsUsed = isUsed,
                IsRevoked = isRevoked,
                ExpiryDate = DateTime.Parse(expiryDateStr)
            };

            _jwtRepoMock.Setup(x => x.GetRefreshTokenAsync(tokenString)).ReturnsAsync(invalidToken);

            // 2. ACT
            var result = await _service.ValidateAndRotateRefreshToken(tokenString);

            // 3. ASSERT
            result.IsValid.Should().BeFalse(because: $"Token should be invalid in case: {caseName}");
            _jwtRepoMock.Verify(x => x.MarkRefreshTokenAsUsed(It.IsAny<RefreshToken>()), Times.Never);
            _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ValidateAndRotate_ShouldDetectReuse_RevokeAllTokens_AndLogIpFromContext()
        {
            // 1. ARRANGE
            var tokenString = "stolen-token";
            var userId = Guid.NewGuid();
            var hackerIp = "203.0.113.45";

            var stolenToken = new RefreshToken
            {
                Token = tokenString,
                UserId = userId,
                IsUsed = true, 
                IsRevoked = false,
                ExpiryDate = DateTime.UtcNow.AddDays(1)
            };

            _jwtRepoMock.Setup(x => x.GetRefreshTokenAsync(tokenString)).ReturnsAsync(stolenToken);
            _userContextMock.Setup(x => x.IpAddress).Returns(hackerIp);

            // 2. ACT
            var result = await _service.ValidateAndRotateRefreshToken(tokenString);

            // 3. ASSERT
            result.IsValid.Should().BeFalse();



            _jwtRepoMock.Verify(x => x.RevokeRefreshTokensForUserAsync(userId), Times.Once);
        }


        [Fact]
        public async Task RevokeRefreshToken_ShouldMarkTokenAsUsed_WhenTokenExists()
        {
            // 1. ARRANGE
            var tokenString = "token-to-revoke";
            var token = new RefreshToken { Token = tokenString };

            _jwtRepoMock.Setup(x => x.GetRefreshTokenAsync(tokenString)).ReturnsAsync(token);

            // 2. ACT
            await _service.RevokeRefreshToken(tokenString);

            // 3. ASSERT
            _jwtRepoMock.Verify(x => x.MarkRefreshTokenAsUsed(token), Times.Once);
            _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RevokeRefreshToken_ShouldDoNothing_WhenTokenDoesNotExist()
        {
            // 1. ARRANGE
            _jwtRepoMock.Setup(x => x.GetRefreshTokenAsync(It.IsAny<string>())).ReturnsAsync((RefreshToken)null);

            // 2. ACT
            await _service.RevokeRefreshToken("nonexistent");

            // 3. ASSERT
            _jwtRepoMock.Verify(x => x.MarkRefreshTokenAsUsed(It.IsAny<RefreshToken>()), Times.Never);
            _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }


        [Fact]
        public async Task GenerateServiceToken_ShouldReturnValidJwt()
        {
            // 1. ARRANGE
            var claims = new List<Claim> { new Claim("service", "example-service") };

            // 2. ACT
            var jwt = await _service.GenerateServiceToken(claims);

            // 3. ASSERT
            jwt.Should().NotBeNullOrEmpty();
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);

            token.Issuer.Should().Be(_authSettings.Issuer);
            token.Claims.Should().Contain(c => c.Type == "service" && c.Value == "example-service");
        }
    }
}