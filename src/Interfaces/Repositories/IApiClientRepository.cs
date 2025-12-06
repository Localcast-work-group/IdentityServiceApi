using IdentityService.Api.Models.ApiClient;

namespace IdentityService.Api.Interfaces.Repositories
{
    public interface IApiClientRepository :IBaseRepository<ApiClient>
    {
        Task<ApiClient?> GetByClientIdAsync(string clientId);
    }
}
