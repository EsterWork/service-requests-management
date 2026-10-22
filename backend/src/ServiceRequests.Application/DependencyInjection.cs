using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ServiceRequests.Application.Services;

namespace ServiceRequests.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<IRequestService, RequestService>();
        services.AddScoped<IRequestSummaryService, RequestSummaryService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // API messages are always English, regardless of the server's OS culture.
        ValidatorOptions.Global.LanguageManager.Enabled = false;

        return services;
    }
}
