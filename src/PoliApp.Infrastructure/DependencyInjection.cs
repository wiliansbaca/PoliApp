using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PoliApp.Infrastructure.Persistence;

namespace PoliApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<PoliAppDbContext>(o =>
            o.UseSqlServer(config.GetConnectionString("PoliAppDB"),
                sql => sql.EnableRetryOnFailure(5))); // Azure SQL serverless puede estar en pausa
        return services;
    }
}
