using JetBrains.Annotations;
using PerformanceTests;
using PerformanceTests.Endpoints.ErrorHandling;
using Traditional.Api.Common.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRequiredDependencies(builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.Map("/", () => Results.Redirect("/swagger/index.html"));

app.MapEndpoints();

app.MapErrorEndpoint();
app.UseExceptionHandler(ErrorEndpoint.ErrorRoute);

await app.RunAsync();

namespace PerformanceTests
{
    /// <summary>
    /// This partial class definition of Program is needed for the integration tests to work.
    /// We have to expose the implicitly defined Program class to the test project.
    /// </summary>
    [UsedImplicitly]
#pragma warning disable S1118, SA1106

    // ReSharper disable once PartialTypeWithSinglePart
    public partial class Program;
}
#pragma warning restore S1118, SA1106
