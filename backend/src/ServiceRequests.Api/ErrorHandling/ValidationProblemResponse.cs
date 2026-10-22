using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace ServiceRequests.Api.ErrorHandling;

/// <summary>
/// Single place that builds every 400 response (model binding, FluentValidation, forbidden transition),
/// with error keys in camelCase to match the rest of the JSON, e.g. "pageSize" or "items[0].rowVersion".
/// </summary>
public static class ValidationProblemResponse
{
    public static BadRequestObjectResult Create(ActionContext context)
    {
        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateValidationProblemDetails(context.HttpContext, context.ModelState);

        var errors = problem.Errors.ToList();
        problem.Errors.Clear();
        foreach (var (key, messages) in errors)
        {
            problem.Errors[ToCamelCasePath(key)] = messages;
        }

        return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
    }

    // JSON paths such as "$.newStatus" are already camelCase and pass through unchanged.
    private static string ToCamelCasePath(string key) =>
        string.Join('.', key.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
