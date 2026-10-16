using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RaceDayPOE.Documentation;

//Adds the 400 / 401 / 403 responses to every Swagger operation that can return them,
//so each endpoint documents the responses it returns.

public sealed class AuthResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata ?? new List<object>();
        var allowsAnonymous = metadata.OfType<IAllowAnonymous>().Any();
        var authorize = metadata.OfType<IAuthorizeData>().ToList();

        if (!allowsAnonymous && authorize.Count > 0)
        {
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized - log in first (session cookie missing or expired)." });
            if (authorize.Any(a => !string.IsNullOrEmpty(a.Policy) || !string.IsNullOrEmpty(a.Roles)))
                operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden - wrong role, or you do not own this record." });
        }

        if (context.ApiDescription.ParameterDescriptions.Any(p => p.Source == BindingSource.Body))
            operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Bad Request - validation failed." });
    }
}
