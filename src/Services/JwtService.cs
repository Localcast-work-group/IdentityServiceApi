using IdentityService.Api.Data;
using IdentityService.Api.Extensions;
using IdentityService.Api.Interfaces;
using IdentityService.Api.Interfaces.Services;
using IdentityService.Api.Models.RefreshToken;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace IdentityService.Api.Services
{
    public class JwtService : IJwtService
    {
        public IUnitOfWork UnitOfWork { get; set; }
        public Serilog.ILogger Logger { get; set; }
        private readonly AuthenticationSettings _authenticationSettings;

        public JwtService(IUnitOfWork unitOfWork, AuthenticationSettings authenticationSettings, ApplicationDbContext applicationDbContext,Serilog.ILogger logger) 
        { 
            _authenticationSettings = authenticationSettings;
            UnitOfWork = unitOfWork;
            Logger = logger;

        }
        public async Task<(string JwtToken, string RefreshToken)> GenerateTokens(Guid userId, IEnumerable<Claim> claims)
        {
            SymmetricSecurityKey symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authenticationSettings.Key));
            SigningCredentials credentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken jwtToken = new JwtSecurityToken(
                _authenticationSettings.Issuer,
                _authenticationSettings.Issuer,
                claims,
                expires: DateTime.UtcNow.AddMinutes(_authenticationSettings.ExpireMinutes),
                signingCredentials: credentials
            );
            string jwt = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            // Generowanie RefreshToken
            string refreshToken = Guid.NewGuid().ToString();
            RefreshToken newRefreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = refreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(_authenticationSettings.RefreshExpireDays),
                CreatedAt = DateTime.UtcNow
            };

            await UnitOfWork.Jwts.AddRefreshTokenAsync(newRefreshToken);

            return (jwt, refreshToken);
        }
        public async Task<(bool IsValid, Guid? UserId)> ValidateAndRotateRefreshToken(string refreshToken, string ipAdress)
        {
            var token = await UnitOfWork.Jwts.GetRefreshTokenAsync(refreshToken);

            if (token == null || token.IsUsed || token.IsRevoked || token.ExpiryDate < DateTime.UtcNow)
            {
                if(token != null && token.IsUsed)
                {
                    Logger.Warning("Refresh token reuse detected for user {UserId} Ip adress: {ipAdress}", token.UserId,ipAdress);
                    await RevokeRefreshTokensForUser(token.UserId);
                }
                return (false, null);
            }
            UnitOfWork.Jwts.MarkRefreshTokenAsUsed(token);
            await UnitOfWork.SaveChangesAsync();
            return (true, token.UserId);
        }
        public async Task RevokeRefreshToken(string refreshToken)
        {
            var token =  await  UnitOfWork.Jwts.GetRefreshTokenAsync(refreshToken);
            if (token != null)
            {
                UnitOfWork.Jwts.MarkRefreshTokenAsUsed(token);
                 await UnitOfWork.SaveChangesAsync();
            }
        }

        public async Task<Guid?> GetUserIdByRefreshToken(string refreshToken)
        {
            return await UnitOfWork.Jwts.GetUserIdByRefreshTokenAsync(refreshToken);
        }

        public async Task RevokeRefreshTokensForUser(Guid userId)
        {
            await UnitOfWork.Jwts.RevokeRefreshTokensForUserAsync(userId);
        }

        public async Task<string> GenerateServiceToken(IEnumerable<Claim> claims)
        {
            SymmetricSecurityKey symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authenticationSettings.Key));
            SigningCredentials credentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken jwtToken = new JwtSecurityToken(
                issuer: _authenticationSettings.Issuer,
                audience: _authenticationSettings.Issuer,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: credentials
            );

            string jwt = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            return jwt;
        }
    }
}
