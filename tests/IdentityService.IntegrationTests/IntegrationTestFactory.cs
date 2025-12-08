using IdentityService.IntegrationTests.Helpers;
using IdentityService.Api;
using IdentityService.Api.Data;
using IdentityService.Contracts.Clients;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Testcontainers.PostgreSql;

namespace IdentityService.IntegrationTests
{
    public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .Build();

        public Mock<IIdentityServiceClient> IdentityServiceMock { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureTestServices(services =>
            {
                var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (dbDescriptor != null) services.Remove(dbDescriptor);
                services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(_dbContainer.GetConnectionString()));



                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "TestScheme";
                    options.DefaultChallengeScheme = "TestScheme";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });
                var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService) &&
                                             d.ImplementationType != null &&
                                             d.ImplementationType.Namespace != null &&
                                             d.ImplementationType.Namespace.Contains("MassTransit")).ToList();

                foreach (var service in hostedServices)
                {
                    services.Remove(service);
                }

                var publishDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPublishEndpoint));
                if (publishDescriptor != null) services.Remove(publishDescriptor);

                var mockPublishEndpoint = new Mock<IPublishEndpoint>();
                services.AddSingleton(mockPublishEndpoint.Object);

                var busDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBus));
                if (busDescriptor != null) services.Remove(busDescriptor);
                services.AddSingleton(new Mock<IBus>().Object);
            });
        }

        public Task InitializeAsync()
        {
            return _dbContainer.StartAsync();
        }

        public new Task DisposeAsync()
        {
            return _dbContainer.StopAsync();
        }
    }

}
