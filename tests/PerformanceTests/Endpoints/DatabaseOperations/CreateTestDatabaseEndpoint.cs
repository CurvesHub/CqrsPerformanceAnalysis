using System.Net;
using Microsoft.AspNetCore.Mvc;
using PerformanceTests.Common.Constants;
using Traditional.Api.Common.DataAccess.Persistence;
using Traditional.Api.Common.Endpoints;

namespace PerformanceTests.Endpoints.DatabaseOperations;

/// <inheritdoc />
public class CreateTestDatabaseEndpoint : IEndpoint
{
    /// <inheritdoc/>
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/database/createMainDb", CreateTestDatabaseAsync)
            .WithTags(EndpointTags.DATABASE_OPERATION)
            .WithSummary("Create the main test database if not existent.")
            .Produces((int)HttpStatusCode.OK)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .WithOpenApi();
    }

    private static async Task<IResult> CreateTestDatabaseAsync(
        [FromServices] TraditionalDbContext dbContext, // We could here also use the CqrsWriteDbContext but since the use the same database scheme it doesn't matter
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
