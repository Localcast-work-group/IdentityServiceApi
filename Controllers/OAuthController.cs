using IdentityService.Api.Interfaces.Services;
using IdentityService.Api.Models.ApiClient.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Controllers
{
    [ApiController]
    [Route("api/oauth")]
    public class OAuthController : ControllerBase
    {
        private readonly IOAuthService _oauthService;

        public OAuthController(IOAuthService oauthService)
        {
            _oauthService = oauthService;
        }

        [HttpPost("token")]
        public async Task<IActionResult> GetToken([FromBody] ClientCredentialsDto request)
        {
            var token = await _oauthService.AuthenticateClientAsync(request.ClientId, request.ClientSecret);

            return Ok(new
            {
                access_token = token,
                token_type = "Bearer",
                expires_in = 3600 
            });
        }
    }
}