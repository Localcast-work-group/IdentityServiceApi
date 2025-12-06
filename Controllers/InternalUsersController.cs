using IdentityService.Api.Enums;
using IdentityService.Api.Interfaces.Services;
using IdentityService.Contracts.ApiResponses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Controllers
{
    [ApiController]
    [Route("api/internal/users")]
    [Authorize(Roles = "ServiceApplication")]
    public class InternalUsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly Serilog.ILogger _logger;

        public InternalUsersController(IUserService userService, Serilog.ILogger logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("{userId}/validate")]
        public async Task<IActionResult> ValidateUser(Guid userId)
        {
            var user = await _userService.GetWithRole(id: userId);

            if (user == null)
            {
                _logger.Information("Internal user check failed: User {UserId} not found", userId);
                return Ok(new UserValidationResponse
                {
                    Exists = false,
                    IsActive = false,
                });
            }

            bool isActive = user.Status != (char)Status.Banned;

            return Ok(new UserValidationResponse
            {
                Exists = true,
                IsActive = isActive,
                CurrentEmail = user.Email,
            });
        }
    }
}