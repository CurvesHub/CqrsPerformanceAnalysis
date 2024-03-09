using System.Net;
using Microsoft.AspNetCore.Mvc;
using Traditional.Api.Common.DataAccess.Persistence;
using Traditional.Api.Common.Endpoints;
using EndpointTags = Traditional.PerformanceTests.Common.Constants.EndpointTags;

namespace Traditional.PerformanceTests.Endpoints.ManualTesting.CreateDatabase;

/// <inheritdoc />
public class CreateTraditionalDatabaseEndpoint : IEndpoint
{
    /// <inheritdoc/>
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/manualTesting/createTraditionalDb", CreateTraditionalDatabaseAsync)
            .WithTags(EndpointTags.TRADITIONAL_DATABASE_OPERATIONS)
            .WithSummary("Create the traditional database if not existent.")
            .Produces((int)HttpStatusCode.OK)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .WithOpenApi();
    }

    private static async Task<IResult> CreateTraditionalDatabaseAsync(
        [FromServices] TraditionalDbContext dbContext,
        bool withCleanup = true)
    {
        if (withCleanup)
        {
            await dbContext.Database.EnsureDeletedAsync();
        }

        await dbContext.Database.EnsureCreatedAsync();
        return Results.Ok();
    }
}
