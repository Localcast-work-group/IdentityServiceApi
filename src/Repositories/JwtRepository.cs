using IdentityService.Api.Data;
using IdentityService.Api.Interfaces.Repositories;
using IdentityService.Api.Models.RefreshToken;
using Microsoft.EntityFrameworkCore;
namespace IdentityService.Api.Repositories
{
    public class JwtRepository : IJwtRepository
    {
        ApplicationDbContext ApplicationDbContext { get; set; }

        public JwtRepository( ApplicationDbContext applicationDbContext) 
        { 
            ApplicationDbContext = applicationDbContext;


        }
        public Task<RefreshToken> GetRefreshTokenAsync(string refreshToken)
        {
            return ApplicationDbContext.RefreshTokens.Where(rt => rt.Token == refreshToken).FirstOrDefaultAsync();

        }

        public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            await ApplicationDbContext.RefreshTokens.AddAsync(refreshToken);
        }

        public void ExpireRefreshToken(RefreshToken refreshToken)
        {
            refreshToken.ExpiryDate = DateTime.UtcNow;
        }
        public async Task<Guid?> GetUserIdByRefreshTokenAsync(string refreshToken)
        {
            var token = await ApplicationDbContext.Set<RefreshToken>()
                              .Where(x => x.Token == refreshToken)
                              .FirstOrDefaultAsync();

            return token?.UserId; 
        }

        public void MarkRefreshTokenAsUsed(RefreshToken refreshToken)
        {
            refreshToken.IsUsed = true;
        }

        public void RevokeRefreshToken(RefreshToken refreshToken)
        {
            refreshToken.IsRevoked = true;
        }
        public async Task RevokeRefreshTokensForUserAsync(Guid userId)
        {
            var tokensToRevoke = await ApplicationDbContext.RefreshTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync(); 

            foreach (var token in tokensToRevoke)
            {
                token.IsRevoked = true;
            }
        }
        public IQueryable<RefreshToken> GetRefreshTokensForUser(Guid userId)
        {
            return ApplicationDbContext.Set<RefreshToken>().Where(x => x.UserId == userId);
        }
    }
}
