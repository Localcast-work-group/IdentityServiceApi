using IdentityService.Api.Interfaces.Models;
using IdentityService.Api.Models.User;
using System.ComponentModel.DataAnnotations;

namespace IdentityService.Api.Models.Role
{
    public class Role : IModelWithNameAndId
    {
        public Guid Id { get; set; }
        [StringLength(100)]
        public string Name { get; set; }
        public virtual ICollection<User.User> Users { get; set; }  = new List<User.User>();
    }
}
