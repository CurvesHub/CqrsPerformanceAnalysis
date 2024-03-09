using Traditional.Api.Common.Endpoints;
using Traditional.PerformanceTests;
using Traditional.PerformanceTests.Endpoints.Common.ErrorHandling;

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
