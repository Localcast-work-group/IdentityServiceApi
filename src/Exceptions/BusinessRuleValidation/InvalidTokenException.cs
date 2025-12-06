namespace IdentityService.Api.Exceptions.BusinessRuleValidation
{
    public class InvalidTokenException : BusinessRuleValidationException
    {
        public string FieldName { get; } = "ResetPasswordToken";
        public InvalidTokenException(string message) : base(message) { }
    }
}
