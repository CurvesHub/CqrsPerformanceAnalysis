using System.Net;
using Microsoft.AspNetCore.Mvc;
using Traditional.Api.Common.Endpoints;
using Traditional.PerformanceTests.Infrastructure;
using EndpointTags = Traditional.PerformanceTests.Common.Constants.EndpointTags;

namespace Traditional.PerformanceTests.Endpoints.ManualTesting.CreateDatabase;

/// <inheritdoc/>
public class CreatePerformanceDatabaseEndpoint : IEndpoint
{
    /// <inheritdoc/>
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/manualTesting/createPerformanceDb", CreatePerformanceDatabaseAsync)
            .WithTags(EndpointTags.PERFORMANCE_DATABASE_OPERATIONS)
            .WithSummary("Create the performance database if not existent.")
            .Produces((int)HttpStatusCode.OK)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .WithOpenApi();
    }

    private static async Task<IResult> CreatePerformanceDatabaseAsync(
        [FromServices] PerformanceDbContext dbContext,
        bool withCleanup = false)
    {
        if (withCleanup)
        {
            await dbContext.Database.EnsureDeletedAsync();
        }

        await dbContext.Database.EnsureCreatedAsync();
        return Results.Ok();
    }
}
