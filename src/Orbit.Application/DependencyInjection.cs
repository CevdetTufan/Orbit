using Microsoft.Extensions.DependencyInjection;
using Orbit.Application.Auth;
using Orbit.Application.Users;
using Orbit.Application.Users.EventHandlers;
using Orbit.Application.Account;
using Orbit.Application.Authorization;
using Orbit.Application.Common;
using Orbit.Domain.Users.Events;

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
        services.AddScoped<IRoleQueries, RoleQueries>();
        services.AddScoped<IRoleCommands, RoleCommands>();
        services.AddScoped<IPermissionQueries, PermissionQueries>();
        services.AddScoped<IRolePermissionCommands, RolePermissionCommands>();

        // Domain Event Infrastructure
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        
        // Domain Event Handlers
        services.AddScoped<IDomainEventHandler<UserDeactivatedEvent>, UserDeactivatedEventHandler>();

        return services;
    }
}
