using IdentityService.Api.Interfaces.Services;
using IdentityService.Api.Models.Role;
using IdentityService.Api.Models.Role.DTOs;
using IdentityService.Api.Models.Role.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly IWebHostEnvironment _environment;

        public RoleController(IRoleService roleService, IWebHostEnvironment environment)
        {
            _roleService = roleService;
        }
        [HttpGet("{id}", Name = "GetRole")]
        public async Task<IActionResult> Get([FromBody] Guid Id)
        {
            Role role= await _roleService.GetById(Id);
            if (role == null)
            {
                return NotFound("Role is null");
            }

            return Ok(role);
        }

    }
}
