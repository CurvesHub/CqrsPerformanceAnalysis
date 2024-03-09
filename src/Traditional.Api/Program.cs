using JetBrains.Annotations;
using Traditional.Api;
using Traditional.Api.Common.Endpoints;
using Traditional.Api.Common.ErrorHandling;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services
    .AddPresentation(configuration)
    .AddApplication()
    .AddInfrastructure(configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.Map("/", () => Results.Redirect("/swagger/index.html"));
}
else
{
    // Exception handling
    app.MapErrorEndpoint();
    app.UseExceptionHandler(ErrorEndpoint.ErrorRoute);
}

app.MapEndpoints();

await app.RunAsync();

namespace Traditional.Api
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
