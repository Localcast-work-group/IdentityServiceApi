namespace IdentityService.Api.Models.User.DTOs
{
    public class ResetPasswordDTO
    {
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public Guid ResetPasswordToken { get; set; }
    }
}
