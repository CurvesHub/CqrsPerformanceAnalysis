using System.Net;
using Microsoft.AspNetCore.Mvc;
using PerformanceTests.Common.Constants;
using PerformanceTests.Infrastructure;
using Traditional.Api.Common.Endpoints;

namespace PerformanceTests.Endpoints.DatabaseOperations;

/// <inheritdoc/>
public class CreatePerformanceDatabaseEndpoint : IEndpoint
{
    /// <inheritdoc/>
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/database/createPerformanceDb", CreatePerformanceDatabaseAsync)
            .WithTags(EndpointTags.DATABASE_OPERATION)
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
