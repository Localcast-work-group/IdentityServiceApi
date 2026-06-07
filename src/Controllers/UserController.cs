using FluentValidation;
using IdentityService.Api.Interfaces.Services;
using IdentityService.Api.Models.RefreshToken.DTOs;
using IdentityService.Api.Models.User;
using IdentityService.Api.Models.User.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IdentityService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]

    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _environment;
        private readonly IJwtService _jwtService;


        public UserController(IUserService userService, IWebHostEnvironment environment, IJwtService jwtService)
        {
            _userService = userService;
            _jwtService = jwtService;
        }
        [HttpPost("")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterUserDTO model)
        {

            User user = await _userService.RegisterUser(model);
            return Created("", new GetUserDTO
            {
                Id = user.Id,
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                FirstName = user.FirstName,
                Role = user.Role.Name
            });
        }
        [HttpGet]
        [Authorize(Roles = "Admin,Course Creator")]
        public async Task<IActionResult> GetAll()
        {

            List<User> users = await _userService.GetAllWithRoles().ToListAsync();
            return Ok(users.Select(u => new GetUserDTO
            {
                Id = u.Id,
                Name = u.Name,
                Surname = u.Surname,
                Email = u.Email,
                FirstName = u.FirstName,
                Role = u.Role.Name

            }));
        }
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginUserDto model)
        {
           

            (string jwt, string refresh) tokens = await _userService.HandleLoginAsync(model);

            Response.Cookies.Append("JWT", tokens.jwt, new CookieOptions
            {
                HttpOnly = true
            });
            return Ok(new { token = tokens.jwt, refreshToken = tokens.refresh });

        }
        [AllowAnonymous]
        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();

            var (isValid, userId) = await _jwtService.ValidateAndRotateRefreshToken(request.RefreshToken);
            if (!isValid)
            {
                return Unauthorized("Invalid or expired refresh token");
            }

            var tokens = await _userService.HandleTokenRefreshAsync(userId.Value);
            Response.Cookies.Append("JWT", tokens.JwtToken, new CookieOptions
            {
                HttpOnly = true
            });
            return Ok(new { token = tokens.JwtToken, refreshToken = tokens.RefreshToken });
        }
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(string refreshToken)
        {
            await _jwtService.RevokeRefreshToken(refreshToken);
            return Ok("Logged out successfully");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}", Name = "GetUser")]
        public async Task<IActionResult> Get([FromBody] Guid id)
        {
            User user = await _userService.GetById(id);
            GetUserDTO model = new GetUserDTO
            {
                Id = user.Id,
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                FirstName = user.FirstName,
                Role = user.Role.Name
            };
            return Ok(model);
        }
        [Authorize]
        [HttpGet("My", Name = "GetMyData")]
        public async Task<IActionResult> GetMyData()
        {
            User user = await _userService.GetWithRole(id: Guid.Parse((User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).FirstOrDefault()).Value) );
            GetUserDTO model = new GetUserDTO
            {
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                FirstName = user.FirstName,
                Role = user.Role.Name
            };
            return Ok(model);
        }
        [HttpGet("IsUserExistsByName", Name = "IsUserExistsByName")]
        public async Task<IActionResult> IsUserExistsByName([FromBody] string name)
        {
            
            return Ok(new {exists =  _userService.IsNameUnique(name) });
        }

        [HttpGet("IsUserExistsByEmail", Name = "IsUserExistsByEmail")]
        public async Task<IActionResult> IsUserExistsByEmail([FromBody] string email)
        {

            return Ok(new { exists = await _userService.IsUserExistsByEmail(email) });
        }

        [HttpPost("GenerateResetPasswordToken", Name = "GenerateResetPasswordToken")]
        public async Task<IActionResult> GenerateResetPasswordToken([FromBody] string email)
        {
            User? user = await _userService.GetByEmail(email);
            if (user != null)
            {
                await _userService.GenerateResetPasswordToken(user);
            }
            return Ok("If an account with this email exists, a password reset link has been sent.");
        }
        [HttpPost("ResetPassword", Name = "ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO resetPasswordDTO)
        {
            await _userService.ResetPassword(resetPasswordDTO.ResetPasswordToken,resetPasswordDTO.Password);
            return Ok();
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("{userId}/role")]
        public async Task<IActionResult> ChangeRole(Guid userId, [FromBody] ChangeRoleDto dto)
        {
            await _userService.ChangeRole(userId, dto.newRoleId);
            return Ok();
        }
    }

}
