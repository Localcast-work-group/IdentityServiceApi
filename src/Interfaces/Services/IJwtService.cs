using IdentityService.Api.Models.RefreshToken;
using System.Security.Claims;
using IdentityService.Api.Models.ApiClient;

namespace IdentityService.Api.Interfaces.Services
{
    public interface IJwtService
    {
        public Task<(bool IsValid, Guid? UserId)> ValidateAndRotateRefreshToken( string refreshToken);
        public Task<(string JwtToken, string RefreshToken)> GenerateTokens(Guid userId, IEnumerable<Claim> claims);
        Task<string> GenerateServiceToken(IEnumerable<Claim> claims);
        public Task RevokeRefreshTokensForUser(Guid userId);
        public Task RevokeRefreshToken(string refreshToken);
        public Task<Guid?> GetUserIdByRefreshToken(string refreshToken);

    }
}
