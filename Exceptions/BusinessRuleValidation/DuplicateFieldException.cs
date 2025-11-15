namespace IdentityService.Api.Exceptions.BusinessRuleValidation
{
    public class DuplicateFieldException : BusinessRuleValidationException
    {
        public string FieldName { get; set; } = "";
        public DuplicateFieldException(string message,string fieldName) : base(message) 
        {
            FieldName = fieldName;
        }
    }
}
