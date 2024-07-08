using Microsoft.EntityFrameworkCore;
using PerformanceTests.Common.Services;
using PerformanceTests.Infrastructure;
using Serilog;
using Traditional.Api.Common.DataAccess.Persistence;
using Traditional.Api.Common.Endpoints;

namespace PerformanceTests;
// ReSharper disable UnusedMethodReturnValue.Local -> Keep the fluent API

/// <summary>
/// Provides the dependency injection configuration.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the dependencies to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    public static void AddRequiredDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        // Swagger
        services
            .AddEndpoints(typeof(DependencyInjection))
            .AddEndpointsApiExplorer()
            .AddSwaggerGen();

        services
            .AddCommonServices()
            .AddSerilog(configuration)
            .AddRequiredDbContexts(configuration);
    }

    private static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        return services
            .AddScoped<ContainerProvider>()
            .AddScoped<TestDataGenerator>()
            .AddScoped<ApiClient>()
            .AddScoped<ResultProcessor>()
            .AddScoped<K6TestHandler>();
    }

    private static IServiceCollection AddSerilog(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
        return services.AddSerilog(Log.Logger);
    }

    private static IServiceCollection AddRequiredDbContexts(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddDbContext<TraditionalDbContext>(OptionsAction("TraditionalConnection"))
            .AddDbContext<PerformanceDbContext>(OptionsAction("PerformanceConnection"));

        Action<DbContextOptionsBuilder> OptionsAction(string connectionStringLocation)
        {
            return options => options.UseNpgsql(configuration.GetConnectionString(connectionStringLocation) + ";Include Error Detail=true");
        }
    }
}
