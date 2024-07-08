using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cqrs.Api.Common.Endpoints;

/// <summary>
/// Defines functionality to extend the <see cref="IServiceCollection"/> and <see cref="IApplicationBuilder"/> for registering and mapping <see cref="IEndpoint"/> implementations.
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Scans the assembly for <see cref="IEndpoint"/> implementations and registers them as transient services.
    /// </summary>
    /// <param name="services">The service collection to add the endpoints to.</param>
    /// <param name="type">The type to scan the assembly of.</param>
    /// <returns>The configured <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Type type)
    {
        var endpointServiceDescriptors = type.Assembly
            .DefinedTypes
            .Where(typeInfo => typeInfo is { IsClass: true, IsAbstract: false, IsInterface: false }
                           && typeInfo.IsAssignableTo(typeof(IEndpoint)))
            .Select(typeInfo => ServiceDescriptor.Transient(typeof(IEndpoint), typeInfo))
            .ToArray();

        services.TryAddEnumerable(endpointServiceDescriptors);

        return services;
    }

    /// <summary>
    /// Maps the registered <see cref="IEndpoint"/> implementations to the provided <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">The web application to resolve the endpoints from.</param>
    /// <param name="routeGroupBuilder">The route group builder to map the endpoints to. If not provided, the endpoints will be mapped to the root of the application.</param>
    /// <returns>The configured <see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder MapEndpoints(
        this WebApplication app,
        RouteGroupBuilder? routeGroupBuilder = null)
    {
        var endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();

        IEndpointRouteBuilder builder = routeGroupBuilder is null
            ? app
            : routeGroupBuilder;

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(builder);
        }

        return app;
    }
}
