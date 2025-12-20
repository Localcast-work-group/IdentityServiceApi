using FluentValidation;
using IdentityService.Api.Exceptions;
using IdentityService.Api.Exceptions.BusinessRuleValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Data;

namespace IdentityService.Api.Filters
{
    public class ApiExceptionFilter : IExceptionFilter
    {
        Serilog.ILogger Logger;

        public ApiExceptionFilter(Serilog.ILogger logger)
        {
            Logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception is AuthenticationFailedException authException)
            {
                HandleAuthenticationException(context, authException);
            }
            else if (context.Exception is BusinessRuleValidationException businessException)
            {
                HandleBusinessRuleException(context, businessException);
            }
            else if (context.Exception is ValidationException  validationException)
            {
                HandleFluentValidationException(context, validationException);
            }
            else if (context.Exception is UnauthorizedAccessException unauthorizedAccessException)
            {
                HandleUnauthorizedAccessException(context, unauthorizedAccessException);
            }
            else
            {
                HandleUnknownException(context);
            }

            
            Logger.Error(context.Exception, "An exception occurred during request processing.");

            context.ExceptionHandled = true;
        }
        private void HandleUnauthorizedAccessException(ExceptionContext context, UnauthorizedAccessException exception)
        {
            var details = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Access denied: " + exception.Message,
                Type = "https.tools.ietf.org/html/rfc7231#section-6.5.3"
            };
            context.Result = new ObjectResult(details)
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
        private void HandleBusinessRuleException(ExceptionContext context, BusinessRuleValidationException exception)
        {
            var details = new ValidationProblemDetails()
            {
                Type = "https.tools.ietf.org/html/rfc7231#section-6.5.8", 
                Title = "A business rule conflict occurred.",
                Status = StatusCodes.Status409Conflict,
                Detail = exception.Message
            };

            string fieldName = "General"; 
            if (exception is DuplicateFieldException exName) fieldName = exName.FieldName;

            details.Errors.Add(fieldName, new[] { exception.Message });
            context.Result = new ObjectResult(details)
            {
                StatusCode = StatusCodes.Status409Conflict
            };
        }

        private void HandleFluentValidationException(ExceptionContext context, ValidationException exception)
        {
            var details = new ValidationProblemDetails(
                exception.Errors.GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                                .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray()))
            {
                Type = "https.tools.ietf.org/html/rfc7231#section-6.5.1", 
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest
            };

            context.Result = new BadRequestObjectResult(details);
        }

        private void HandleUnknownException(ExceptionContext context)
        {
            var details = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Type = "https.tools.ietf.org/html/rfc7231#section-6.6.1"
            };

            context.Result = new ObjectResult(details)
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
        private void HandleAuthenticationException(ExceptionContext context, AuthenticationFailedException exception)
        {
            var details = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication Failed",
                Detail = exception.Message, 
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
            };

            context.Result = new ObjectResult(details)
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
        }
    }
}
