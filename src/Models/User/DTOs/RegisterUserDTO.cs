using System.ComponentModel.DataAnnotations;

namespace IdentityService.Api.Models.User.DTOs
{
    public class RegisterUserDTO
    {
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
