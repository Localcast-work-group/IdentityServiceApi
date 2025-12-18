using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;


namespace IdentityService.Api.Extensions
{

    public class AuthRequirementsOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var authAttributes = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
                .Union(context.MethodInfo.GetCustomAttributes(true))
                .OfType<AuthorizeAttribute>();

            if (authAttributes.Any())
            {
                var requiredRoles = authAttributes
                    .Where(a => !string.IsNullOrEmpty(a.Roles))
                    .Select(a => a.Roles)
                    .OrderBy(r => r)
                    .ToList();

                if (requiredRoles.Any())
                {
                    var rolesStr = string.Join(", ", requiredRoles);
                    operation.Description += $"<br/><b>Wymagane role:</b> {rolesStr}";
                }
                else
                {
                    operation.Description += "<br/><b>Wymagane uwierzytelnienie (dowolna rola)</b>";
                }

                var scheme = new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } };
                operation.Security = new List<OpenApiSecurityRequirement>
                    {
                new OpenApiSecurityRequirement
                {
                    [scheme] = new List<string>()
                }
                    };
            }
        }
    }
}
