using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Traditional.Api.Common.DataAccess.Persistence;
using Traditional.Api.Common.DataAccess.Repositories;
using Traditional.Api.Common.Endpoints;
using Traditional.Api.Common.ErrorHandling;
using Traditional.Api.Common.Interfaces;
using Traditional.Api.UseCases.Articles.Persistence.Repositories;
using Traditional.Api.UseCases.Attributes.Common.Persistence.Entities;
using Traditional.Api.UseCases.Attributes.Common.Persistence.Repositories;
using Traditional.Api.UseCases.Attributes.Common.Services;
using Traditional.Api.UseCases.Attributes.GetAttributes;
using Traditional.Api.UseCases.Attributes.GetLeafAttributes;
using Traditional.Api.UseCases.Attributes.GetSubAttributes;
using Traditional.Api.UseCases.Attributes.UpdateAttributeValues;
using Traditional.Api.UseCases.Categories.Common.Persistence.Repositories;
using Traditional.Api.UseCases.Categories.GetCategoryMapping;
using Traditional.Api.UseCases.Categories.GetChildrenOrTopLevel;
using Traditional.Api.UseCases.Categories.SearchCategories;
using Traditional.Api.UseCases.Categories.UpdateCategoryMapping;
using Traditional.Api.UseCases.RootCategories.Common.Persistence.Entities;
using Traditional.Api.UseCases.RootCategories.GetRootCategories;

namespace Traditional.Api;

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
        services.AddScoped<GetCategoryMappingHandler>();
        services.AddScoped<GetChildrenOrTopLevelHandler>();
        services.AddScoped<SearchCategoriesHandler>();
        services.AddScoped<UpdateCategoryMappingHandler>();
        services.AddScoped<GetRootCategoriesHandler>();

        // Add attribute handlers
        services.AddScoped<GetAttributesHandler>();
        services.AddScoped<UpdateAttributeValuesHandler>();
        services.AddScoped<GetLeafAttributesHandler>();
        services.AddScoped<GetSubAttributesHandler>();

        // Add Services
        services.AddScoped<NewAttributeValueValidationService>();
        services.AddScoped<AttributeService>();
        services.AddScoped<AttributeConverter>();

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
        services.AddDbContext(configuration);

        // Add caches
        services.AddMemoryCache();
        services.AddSingleton<Cache<RootCategory>>();
        services.AddSingleton<Cache<AttributeMapping>>();

        // Add article repository
        services.AddScoped<IArticleRepository, ArticleRepository>();

        // Add category repositories
        services.AddScoped<ICachedRepository<RootCategory>, CachedRepository<RootCategory>>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        // Add attribute repositories
        services.AddScoped<IAttributeRepository, AttributeRepository>();
        services.AddScoped<ICachedRepository<AttributeMapping>, CachedRepository<AttributeMapping>>();

        return services;
    }

    private static void AddDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        // Add db context
        var connectionString = string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase)
            ? configuration.GetConnectionString("DockerConnection")
            : configuration.GetConnectionString("LocalConnection");

        // Optional: Verify if the QuerySplittingBehavior is good for all queries if not just enable it for the specific queries by calling .AsSplitQuery()
        services.AddDbContext<TraditionalDbContext>(options =>
            options.UseNpgsql(connectionString, config =>
                config.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
    }
}
