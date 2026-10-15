using Microsoft.Extensions.DependencyInjection;
using TitoRegistry.Application.Services;

namespace TitoRegistry.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IServiceRegistryService, ServiceRegistryService>();
        return services;
    }
}
