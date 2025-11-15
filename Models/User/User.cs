using IdentityService.Api.Interfaces.Models;
using IdentityService.Api.Models.Role;
using System.ComponentModel.DataAnnotations;

namespace IdentityService.Api.Models.User
{
    public class User : IModelWithNameAndId
    {
        public Guid Id { get; set; }
        [StringLength(150)]
        [Required]
        public string Name { get; set; }
        [StringLength(150)]
        [Required]
        public string FirstName { get; set; }
        [StringLength(150)]
        [Required]
        public string Surname { get; set; }
        [StringLength(150)]
        [Required]
        public string Email { get; set; }
        [StringLength(150)]
        [Required]
        public string PasswordHash { get; set; }
        public Guid? ResetPasswordToken { get; set; }
        public Guid? ConfirmationToken { get; set; }
        public DateTime? ResetPasswordTokenExpiry { get; set; }
        public char Status {  get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastPasswordChangedAt { get; set; }
        public Guid RoleId { get; set; }
        public virtual Role.Role Role { get; set; }

    }
}
