using IdentityService.Api.Models.RefreshToken;

namespace IdentityService.Api.Interfaces.Repositories
{
    public interface IJwtRepository
    {
        Task<RefreshToken> GetRefreshTokenAsync( string refreshToken);
        void RevokeRefreshToken(RefreshToken refreshToken);
        Task RevokeRefreshTokensForUserAsync(Guid userId);
        Task AddRefreshTokenAsync(RefreshToken refreshToken);
        void MarkRefreshTokenAsUsed(RefreshToken refreshToken);
        void ExpireRefreshToken(RefreshToken refreshToken);
        Task<Guid?> GetUserIdByRefreshTokenAsync(string refreshToken);
        IQueryable<RefreshToken> GetRefreshTokensForUser(Guid userId);

    }
}
