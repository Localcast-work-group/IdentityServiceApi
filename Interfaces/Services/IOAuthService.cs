namespace IdentityService.Api.Interfaces.Services
{
    public interface IOAuthService
    {
        Task<string> AuthenticateClientAsync(string clientId, string clientSecret);
    }
}
