using Microsoft.Extensions.DependencyInjection;
using Orbit.Application.Auth;
using Orbit.Application.Users;

namespace Orbit.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Application-layer services
        services.AddScoped<IUserCommands, UserCommands>();
        services.AddScoped<IUserQueries, UserQueries>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ILoginAttemptQueries, LoginAttemptQueries>();
        return services;
    }
}

