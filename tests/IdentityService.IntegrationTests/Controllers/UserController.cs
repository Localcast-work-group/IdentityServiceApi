using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using IdentityService.Api.Data;
using IdentityService.Api.Models.User;
using IdentityService.Api.Models.User.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.IntegrationTests.Controllers
{
    public class UserControllerTests : IClassFixture<IntegrationTestFactory>
    {
        private readonly HttpClient _client;
        private readonly IntegrationTestFactory _factory;

        public UserControllerTests(IntegrationTestFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Register_ShouldCreateUser_WhenDataIsValid()
        {
            // 1. ARRANGE
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await db.Database.EnsureCreatedAsync();
            }

            var registerDto = new RegisterUserDTO
            {
                Email = "integration@test.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                Name = "integrationUser",
                FirstName = "John",
                Surname = "Doe"
            };

            // 2. ACT
            var response = await _client.PostAsJsonAsync("/api/User", registerDto);

            // 3. ASSERT
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"API Error: {response.StatusCode} - {error}");
            }

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);

                user.Should().NotBeNull();
                user!.Name.Should().Be(registerDto.Name);

                var role = await db.Roles.FindAsync(user.RoleId);
                role.Should().NotBeNull();
                role!.Name.Should().Be("User");
            }
        }

        [Fact]
        public async Task Login_ShouldReturnTokens_WhenCredentialsAreValid()
        {
            // 1. ARRANGE
            var password = "Password123!";
            var email = "login@test.com";

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await db.Database.EnsureCreatedAsync();

                // Musimy mieć rolę
                if (!db.Roles.Any(x => x.Name == "User"))
                {
                    var newRole = new IdentityService.Api.Models.Role.Role { Name = "User" };
                    db.Roles.Add(newRole);
                    await db.SaveChangesAsync();
                }
                var role = await db.Roles.FirstAsync(r => r.Name == "User");
                var user = new User
                {
                    Email = email,
                    FirstName = "Test",
                    Surname = "User",
                    CreatedAt = DateTime.UtcNow,
                    Name = "LoginUser",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    RoleId = role.Id,
                    Status = 'N'
                };
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }

            var loginDto = new LoginUserDto { Email = email, Password = password };

            // 2. ACT
            var response = await _client.PostAsJsonAsync("/api/User/login", loginDto);

            // 3. ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<LoginResponse>();

            content.Should().NotBeNull();
            content!.Token.Should().NotBeNullOrEmpty();
            content.RefreshToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task RefreshToken_ShouldReturnNewTokens_WhenRefreshTokenIsValid()
        {
            // 1. ARRANGE
            var userId = Guid.NewGuid();
            var refreshTokenString = Guid.NewGuid().ToString();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await db.Database.EnsureCreatedAsync();
                if (!db.Roles.Any(x => x.Name == "User"))
                {
                    var newRole = new IdentityService.Api.Models.Role.Role { Name = "User" };
                    db.Roles.Add(newRole);
                    await db.SaveChangesAsync();
                }
                var role = await db.Roles.FirstAsync(r => r.Name == "User");


                var user = new User 
                { Id = userId,
                    Email = "refresh@test.com",
                    RoleId = role.Id,
                    Name = "RefreshUser", 
                    PasswordHash = "hash",
                    CreatedAt = DateTime.UtcNow,
                    Surname = "Surname",
                    FirstName = "FirstName",
                    Status = 'N',
                    
                };
                db.Users.Add(user);

                db.RefreshTokens.Add(new IdentityService.Api.Models.RefreshToken.RefreshToken
                {
                    Token = refreshTokenString,
                    UserId = userId,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    IsUsed = false,
                    IsRevoked = false
                });
                await db.SaveChangesAsync();
            }

            // 2. ACT
            var requestBody = new { RefreshToken = refreshTokenString };
            var response = await _client.PostAsJsonAsync("/api/User/RefreshToken", requestBody);

            // 3. ASSERT
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception(err);
            }
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
            content.Should().NotBeNull();
            content!.Token.Should().NotBeNullOrEmpty();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var oldToken = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshTokenString);
                oldToken!.IsUsed.Should().BeTrue();
            }
        }

        [Fact]
        public async Task GetMyData_ShouldReturnUserData_WhenAuthorized()
        {
            // 1. ARRANGE

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestScheme");

            var userIdFromHandler = Guid.Parse("11111111-1111-1111-1111-111111111111");

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await db.Database.EnsureCreatedAsync();
                if(!db.Roles.Any(x => x.Name == "User"))
                {
                    var newRole = new IdentityService.Api.Models.Role.Role { Name = "User" };
                    db.Roles.Add(newRole);
                    await db.SaveChangesAsync();
                }
                var role = await db.Roles.FirstAsync(r => r.Name == "User");

                // Dodajemy usera, którego "udaje" nasz TestAuthHandler
                if (!await db.Users.AnyAsync(u => u.Id == userIdFromHandler))
                {
                    db.Users.Add(new User
                    {
                        Id = userIdFromHandler,
                        Email = "me@test.com",
                        Name = "MyName",
                        FirstName = "Me",
                        Surname = "Myself",
                        Status = 'N',
                        RoleId = role.Id,
                        PasswordHash = "hash"
                    });
                    await db.SaveChangesAsync();
                }
            }

            // 2. ACT
            var response = await _client.GetAsync("/api/User/my");

            // 3. ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var userData = await response.Content.ReadFromJsonAsync<GetUserDTO>();

            userData.Should().NotBeNull();
            userData!.Email.Should().Be("me@test.com");
        }
    }

    public class LoginResponse
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }
}