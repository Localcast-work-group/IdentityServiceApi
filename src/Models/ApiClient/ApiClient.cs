using IdentityService.Api.Interfaces.Models;
using System.ComponentModel.DataAnnotations;

namespace IdentityService.Api.Models.ApiClient
{
    public class ApiClient : IModelWithNameAndId
    {
        public Guid Id { get; set; }

        [Required]
        public string ClientId { get; set; } 

        [Required]
        public string ClientSecretHash { get; set; } 

        public string Name { get; set; } 

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}