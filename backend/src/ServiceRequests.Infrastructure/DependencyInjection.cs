using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceRequests.Application.Interfaces;
using ServiceRequests.Infrastructure.Persistence;
using ServiceRequests.Infrastructure.Seeding;

namespace ServiceRequests.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ServiceRequestsDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ServiceRequestsDbContext>());
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
