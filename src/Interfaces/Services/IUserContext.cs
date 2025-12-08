using System.Security.Claims;

namespace IdentityService.Api.Interfaces.Services
{
    public interface IUserContext
    {
        Guid? UserId { get; }
        ClaimsPrincipal User { get; }
        string? IpAddress { get; }

    }
}
