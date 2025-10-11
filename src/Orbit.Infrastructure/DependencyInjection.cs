using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orbit.Domain.Common;
using Orbit.Infrastructure.Persistence;
using Orbit.Infrastructure.Persistence.Repositories;

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

        return services;
    }
}

