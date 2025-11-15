namespace IdentityService.Api.Exceptions
{
    public abstract class BusinessRuleValidationException : Exception
    {
        public BusinessRuleValidationException(string message) : base(message) { }
    }
}
