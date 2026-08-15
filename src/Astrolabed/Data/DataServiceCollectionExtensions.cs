using System;

using Astrolabed;
using Astrolabed.Data.Repositories;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Astrolabed.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddDataServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        var dbSection = config.GetSection(DbOptions.SectionName);
        services.Configure<DbOptions>(dbSection);

        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddSingleton<IDnsResponseEventRepository, DapperDnsResponseEventRepository>();

        return services;
    }
}
