using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceRequests.Infrastructure.Persistence;

namespace ServiceRequests.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ServiceRequestsDbContext>(options => options.UseSqlServer(connectionString));

        return services;
    }
}
