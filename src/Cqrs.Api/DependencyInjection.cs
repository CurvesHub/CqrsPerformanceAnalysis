using Cqrs.Api.Common.DataAccess.Persistence;
using Cqrs.Api.Common.DataAccess.Repositories;
using Cqrs.Api.Common.Endpoints;
using Cqrs.Api.Common.ErrorHandling;
using Cqrs.Api.UseCases.Attributes.Commands.UpdateAttributeValues;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities;
using Cqrs.Api.UseCases.Attributes.Common.Services;
using Cqrs.Api.UseCases.Attributes.Queries.GetAttributes;
using Cqrs.Api.UseCases.Attributes.Queries.GetLeafAttributes;
using Cqrs.Api.UseCases.Attributes.Queries.GetSubAttributes;
using Cqrs.Api.UseCases.Categories.Commands.UpdateCategoryMapping;
using Cqrs.Api.UseCases.Categories.Queries.GetCategoryMapping;
using Cqrs.Api.UseCases.Categories.Queries.GetChildrenOrTopLevel;
using Cqrs.Api.UseCases.Categories.Queries.SearchCategories;
using Cqrs.Api.UseCases.RootCategories.Common.Persistence.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Cqrs.Api;

// ReSharper disable UnusedMethodReturnValue.Local

/// <summary>
/// Provides the dependency injection configuration.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the dependencies of the presentation layer to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The current service collection.</returns>
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        // Swagger
        services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen();

        services
            .AddEndpoints(typeof(DependencyInjection))
            .AddScoped<HttpProblemDetailsService>();

        // Add Serilog
        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
        services.AddSerilog(Log.Logger);

        return services;
    }

    /// <summary>
    /// Adds the dependencies of the application layer to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The current service collection.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Add fluent validators
        services.AddValidatorsFromAssemblyContaining(typeof(DependencyInjection));

        // Add category handlers
        services.AddScoped<GetCategoryMappingQueryHandler>();
        services.AddScoped<GetChildrenOrTopLevelQueryHandler>();
        services.AddScoped<SearchCategoriesQueryHandler>();
        services.AddScoped<UpdateCategoryMappingCommandHandler>();

        // Add attribute handlers
        services.AddScoped<GetAttributesQueryHandler>();
        services.AddScoped<UpdateAttributeValuesCommandHandler>();
        services.AddScoped<GetLeafAttributesQueryHandler>();
        services.AddScoped<GetSubAttributesQueryHandler>();

        // Add Services
        services.AddScoped<AttributeReadService>();

        return services;
    }

    /// <summary>
    /// Adds the dependencies of the infrastructure layer to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The current service collection.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRequiredDbContexts(configuration);

        // Add caches
        services.AddMemoryCache();
        services.AddSingleton<Cache<RootCategory>>();
        services.AddSingleton<Cache<AttributeMapping>>();

        // Add cached repositories
        services.AddScoped<ICachedReadRepository<RootCategory>, CachedReadRepository<RootCategory>>();
        services.AddScoped<ICachedReadRepository<AttributeMapping>, CachedReadRepository<AttributeMapping>>();

        return services;
    }

    private static IServiceCollection AddRequiredDbContexts(this IServiceCollection services, IConfiguration configuration)
    {
        // Add db context
        var connectionString = string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase)
            ? configuration.GetConnectionString("DockerConnection")
            : configuration.GetConnectionString("LocalConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The connection string is not set.");
        }

        return services
            .AddDbContext<CqrsWriteDbContext>(OptionsAction(connectionString))
            .AddDbContext<CqrsReadDbContext>(OptionsAction(connectionString));

        // Optional: Verify if the QuerySplittingBehavior is good for all queries if not just enable it for the specific queries by calling .AsSplitQuery()
        Action<DbContextOptionsBuilder> OptionsAction(string connectionStringToUse)
        {
            return options => options.UseNpgsql(
                connectionString: connectionStringToUse + ";Include Error Detail=true",
                npgsqlOptionsAction: config => config.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
        }
    }
}
