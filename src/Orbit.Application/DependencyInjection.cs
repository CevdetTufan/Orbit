using Microsoft.Extensions.DependencyInjection;

namespace Orbit.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register application-layer services (e.g., MediatR, validators) here when available.
        return services;
    }
}

