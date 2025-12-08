using IdentityService.Api.Interfaces.Services;
using System.Security.Claims;

namespace IdentityService.Api.Services
{
    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _accessor;

        public UserContext(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public ClaimsPrincipal User => _accessor.HttpContext?.User;
        public bool IsServiceRequest => _accessor.HttpContext?.User?.IsInRole("ServiceApplication") ?? false;
        public Guid? UserId
        {
            get
            {
                var subClaim = _accessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                               ?? _accessor.HttpContext?.User?.FindFirst("sub")?.Value;

                if (Guid.TryParse(subClaim, out var userId))
                {
                    return userId;
                }

                return Guid.Empty;
            }
        }
        public string? IpAddress => GetIpAddress();

        private string? GetIpAddress()
        {
            var context = _accessor.HttpContext;
            if (context == null) return null;

            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                return forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString();
        }
    }
}

