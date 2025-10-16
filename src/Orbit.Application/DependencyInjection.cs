using Microsoft.Extensions.DependencyInjection;
using Orbit.Application.Auth;
using Orbit.Application.Users;
using Orbit.Application.Account;

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
        services.AddScoped<IAccountService, AccountService>();
        return services;
    }
}
