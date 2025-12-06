namespace IdentityService.Api.Models.ApiClient.DTOs
{
    public record ClientCredentialsDto
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}
