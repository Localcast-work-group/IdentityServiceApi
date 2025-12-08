using IdentityService.Api.Exceptions;
using IdentityService.Api.Interfaces;
using IdentityService.Api.Interfaces.Services;
using IdentityService.Api.Models;
using IdentityService.Api.Models.ApiClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace IdentityService.Api.Services
{
    public class OAuthService : IOAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtService _jwtService;
        private readonly Serilog.ILogger _logger;

        public OAuthService(IUnitOfWork unitOfWork, IJwtService jwtService, Serilog.ILogger logger)
        {
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<string> AuthenticateClientAsync(string clientId, string clientSecret)
        {
            ApiClient? client = await _unitOfWork.ApiClients.GetByClientIdAsync(clientId);

            if (client == null || !BCrypt.Net.BCrypt.Verify(clientSecret, client.ClientSecretHash))
            {
                throw new AuthenticationFailedException("Invalid ClientId or ClientSecret");
            }

            if (!client.IsActive)
            {
                _logger.Warning("Authentication attempt by deactivated client: {ClientId}", clientId);
                throw new AuthenticationFailedException("Client is deactivated");
            }
            _logger.Information("Successfully authenticated service: {ClientId}", clientId);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, client.ClientId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        
                new Claim(ClaimTypes.Role, "ServiceApplication"), 
        
                new Claim(ClaimTypes.Name, client.Name ?? client.ClientId)
            };
            string token = await _jwtService.GenerateServiceToken(claims);

            return token;
        }
    }
}