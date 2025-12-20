namespace IdentityService.Api.Models.User.DTOs
{
    public record ChangeRoleDto
    {
        public Guid newRoleId { get; set; }
    }
}
