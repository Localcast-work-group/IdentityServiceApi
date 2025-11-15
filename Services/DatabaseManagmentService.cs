using IdentityService.Api.Data;
using IdentityService.Api.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Services
{
    public static class DatabaseManagmentService
    {
        public static async void MigrationInitialisation(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
                var service = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();
                await service.Database.MigrateAsync();
                var userService = serviceScope.ServiceProvider.GetService<IUserService>();
                await userService.RegisterAdmin();
            }
        }
    }
}
