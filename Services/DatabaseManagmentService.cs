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
                //api client seed
                if(!service.ApiClients.Any(x=> x.ClientId == "CourseService") )
                {
                    service.ApiClients.Add(new Models.ApiClient.ApiClient
                    {
                        Id = Guid.NewGuid(),
                        ClientId = "CourseService",
                        IsActive = true,
                        Name = "Course Service Client",
                        CreatedAt = DateTime.UtcNow,
                        ClientSecretHash = BCrypt.Net.BCrypt.HashPassword("super-secret-key")
                    });

                }
                if (!service.ApiClients.Any(x => x.ClientId == "VideoService"))
                {
                    service.ApiClients.Add(new Models.ApiClient.ApiClient
                    {
                        Id = Guid.NewGuid(),
                        ClientId = "VideoService",
                        IsActive = true,
                        Name = "Video Service Client",
                        CreatedAt = DateTime.UtcNow,
                        ClientSecretHash = BCrypt.Net.BCrypt.HashPassword("super-secret-key")
                    });

                }
                if (!service.ApiClients.Any(x => x.ClientId == "CommentService"))
                {
                    service.ApiClients.Add(new Models.ApiClient.ApiClient
                    {
                        Id = Guid.NewGuid(),
                        ClientId = "CommentService",
                        IsActive = true,
                        Name = "Comment Service Client",
                        CreatedAt = DateTime.UtcNow,
                        ClientSecretHash = BCrypt.Net.BCrypt.HashPassword("super-secret-key")
                    });

                }
                await service.SaveChangesAsync();
            }
        }
    }
}
