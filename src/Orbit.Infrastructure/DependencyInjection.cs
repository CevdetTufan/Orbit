using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orbit.Application.Auth;
using Orbit.Domain.Common;
using Orbit.Infrastructure.Persistence;
using Orbit.Infrastructure.Persistence.Entities;
using Orbit.Infrastructure.Persistence.Repositories;
using Orbit.Infrastructure.Security;
using Orbit.Infrastructure.Seeding;

namespace Orbit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDb)
    {
        if (configureDb is null) throw new ArgumentNullException(nameof(configureDb));

        services.AddDbContext<AppDbContext>(configureDb);

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped(typeof(IReadRepository<,>), typeof(EfRepository<,>));
        services.AddScoped(typeof(IWriteRepository<,>), typeof(EfRepository<,>));
        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));

        // Security services
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IUserCredentialStore, UserCredentialStore>();

        // Data seeding
        services.AddScoped<IDataSeeder, UserDataSeeder>();

        return services;
    }

    public static IServiceCollection AddJwt(this IServiceCollection services, Action<JwtOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<ITokenService, TokenService>();
        return services;
    }
}
