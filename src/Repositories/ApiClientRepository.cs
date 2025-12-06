using IdentityService.Api.Data;
using IdentityService.Api.Interfaces.Repositories;
using IdentityService.Api.Models.ApiClient;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Repositories
{
    public class ApiClientRepository : BaseRepository<ApiClient>, IApiClientRepository
    {
        private readonly ApplicationDbContext _context;

        public ApiClientRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<ApiClient?> GetByClientIdAsync(string clientId)
        {
            return await _context.ApiClients
                .AsNoTracking() 
                .FirstOrDefaultAsync(c => c.ClientId == clientId);
        }
    }
}